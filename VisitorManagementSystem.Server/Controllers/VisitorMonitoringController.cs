using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.IO.Compression;
using System.Text;
using VisitorManagementSystem.Server.Data;
using VisitorManagementSystem.Server.Models;
using VisitorManagementSystem.Server.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System.Net.NetworkInformation;

namespace VisitorManagementSystem.Server.Controllers
{
    [ApiController]
    [Route("api/VisitorMonitoring")]
    public class VisitorMonitoringController : ControllerBase
    {
        private readonly VisitorDBContext _context;
        private readonly IEmailQueue _emailQueue;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public VisitorMonitoringController(VisitorDBContext context, IEmailQueue emailQueue, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _emailQueue = emailQueue;
            _config = config;
            _env = env;
        }


        [HttpPost("schedule-visit")]
        public async Task<IActionResult> ScheduleVisit([FromBody] RegistrationDTO dto)
        {
            if (dto == null) return BadRequest("Request body cannot be empty.");

            // Validate Branch
            var branchExists = await _context.Branches.AnyAsync(b => b.Id == dto.BranchId);
            if (!branchExists) return BadRequest($"Branch ID {dto.BranchId} not found.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Handle the Registrant (Organizer)
                var registrant = await _context.Registrants
                    .FirstOrDefaultAsync(r => r.Email == dto.RegistrantEmail);

                if (registrant == null)
                {
                    // If the image is null/empty, handle it gracefully
                    string registrantImagePath = !string.IsNullOrEmpty(dto.RegistrantValidID)
                        ? await SaveBase64Image(dto.RegistrantValidID, "Registrants")
                        : "default_id.png";

                    registrant = new Registrant
                    {
                        FirstName = dto.RegistrantFirstName,
                        MiddleName = dto.RegistrantMiddleName,
                        Prefix = dto.RegistrantPrefix,
                        Suffix = dto.RegistrantSuffix,
                        LastName = dto.RegistrantLastName,
                        Email = dto.RegistrantEmail,
                        RegistrantValidID = registrantImagePath,
                        Organization = dto.RegistrantOrganization,
                        Designation = dto.RegistrantDesignation
                    };
                    _context.Registrants.Add(registrant);
                    await _context.SaveChangesAsync();
                }

                // 2. Pre-process Vehicles (Save images and generate codes ONCE)
                var vehicleTemplates = new List<VehiclePass>();
                if (dto.Vehicles != null && dto.Vehicles.Any())
                {
                    foreach (var veh in dto.Vehicles)
                    {
                        string vehicleImagePath = !string.IsNullOrEmpty(veh.ValidIdPath)
                            ? await SaveBase64Image(veh.ValidIdPath, "vehicles")
                            : "default_vehicle.png";

                        vehicleTemplates.Add(new VehiclePass
                        {
                            VehicleQrCode = GenerateCode("CAR"), // The SAME code for all visitors
                            Prefix = veh.Prefix,
                            FirstName = veh.FirstName,
                            MiddleName = veh.MiddleName,
                            LastName = veh.LastName,
                            Suffix = veh.Suffix,
                            VehiclePlateNo = veh.PlateNo,
                            VehicleModel = veh.Model,
                            VehicleColor = veh.Color,
                            ValidIdPath = vehicleImagePath
                        });
                    }
                }

                var generatedCodes = new List<object>();

                // 3. Loop through each Visitor
                if (dto.Visitors == null) return BadRequest("At least one visitor is required.");

                foreach (var vDto in dto.Visitors)
                {
                    string visitorImagePath = !string.IsNullOrEmpty(vDto.ValidId)
                        ? await SaveBase64Image(vDto.ValidId, "visitors")
                        : "default_visitor.png";

                    var registration = new Registration
                    {
                        RegistrationCode = GenerateCode("REG"), // Unique QR per person
                        VisitDateTime = dto.VisitDateTime,
                        PersonToVisit = dto.PersonToVisit,
                        PurposeOfVisit = dto.PurposeOfVisit,
                        BranchId = dto.BranchId,
                        RegistrantId = registrant.Id,
                        VisitStatus = 0,
                        CreatedAt = DateTime.Now,
                        Visitors = new List<Visitor>
                {
                    new Visitor
                    {
                        FirstName = vDto.FirstName,
                        MiddleName = vDto.MiddleName,
                        LastName = vDto.LastName,
                        Prefix = vDto.Prefix,
                        Suffix = vDto.Suffix,
                        Email = vDto.Email,
                        Designation = vDto.Designation,
                        Department = vDto.Department,
                        ValidIdPath = visitorImagePath,
                        Organization = vDto.Organization,
                    }
                },
                        // Attach the shared vehicle data
                        Vehicles = vehicleTemplates.Select(v => new VehiclePass
                        {
                            VehicleQrCode = v.VehicleQrCode, // REUSE the same code
                            Prefix = v.Prefix,
                            FirstName = v.FirstName,
                            MiddleName = v.MiddleName,
                            LastName = v.LastName,
                            Suffix = v.Suffix,
                            VehiclePlateNo = v.VehiclePlateNo,
                            VehicleModel = v.VehicleModel,
                            VehicleColor = v.VehicleColor,
                            ValidIdPath = v.ValidIdPath // REUSE the same file path
                        }).ToList()
                    };

                    _context.Registrations.Add(registration);

                    generatedCodes.Add(new
                    {
                        Visitor = $"{vDto.FirstName} {vDto.LastName}",
                        Code = registration.RegistrationCode,
                        // Include car code in response so the UI can show it
                        VehicleCodes = vehicleTemplates.Select(vt => vt.VehicleQrCode).ToList()
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Registrations created successfully. An email will be sent once approved.",
                    details = generatedCodes
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the full exception for debugging
                return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
            }
        }

        [HttpGet("retrieve/visitor/{qrCode}")]
        public async Task<IActionResult> GetVisitorByQr(string qrCode)
        {
            var reg = await _context.Registrations
                .Include(r => r.Visitors)
                .Include(r => r.Branch)
                .Where(r => r.IsApproved == true)
                .FirstOrDefaultAsync(r => r.RegistrationCode == qrCode);

            if (reg == null)
                return NotFound(new { message = "Invalid Visitor QR Code." });

            var visitor = reg.Visitors.FirstOrDefault();
            if (visitor == null)
                return NotFound(new { message = "No visitor data found for this code." });

            return Ok(new
            {
                Type = "Visitor Pass",
                // IDENTITY
                visitor.FirstName,
                visitor.MiddleName,
                visitor.LastName,
                visitor.Prefix,
                visitor.Suffix,
                visitor.Designation,
                //visitor.Department,
                reg.RegistrationCode,
                ValidIdBase64 = GetImageAsBase64(visitor.ValidIdPath),

                // VISIT INFO (Add Organization here!)
                Organization = visitor.Organization,
                VisitDateTime = reg.VisitDateTime,

                // BRANCH INFO
                BranchName = reg.Branch != null ? reg.Branch.BranchName : "N/A",
                Location = reg.Branch != null ? reg.Branch.Location : "N/A"
            });
        }

        [HttpGet("retrieve/vehicle/{carQrCode}")]
        public async Task<IActionResult> GetVehicleByQr(string carQrCode)
        {
            // 1. Find all registrations linked to this specific Car QR
            var registrationsWithThisCar = await _context.Registrations
                .Include(r => r.Visitors)
                .Include(r => r.Vehicles)
                .Include(r => r.Branch) // Ensure Branch is included
                .Where(r => r.Vehicles.Any(v => v.VehicleQrCode == carQrCode) && r.IsApproved == true)
                .ToListAsync();

            if (!registrationsWithThisCar.Any())
                return NotFound(new { message = "Invalid Vehicle Pass." });

            // 2. Extract the car details from the first matching record
            var vehicle = registrationsWithThisCar
                .SelectMany(r => r.Vehicles)
                .FirstOrDefault(v => v.VehicleQrCode == carQrCode);

            // 3. Extract all unique visitors from these registrations
            var passengers = registrationsWithThisCar
            .SelectMany(reg => reg.Visitors.Select(v => new
            {
                // Pull from the parent Registration table
                RegistrationCode = reg.RegistrationCode,

                // Pull from the child Visitor table
                v.FirstName,
                v.MiddleName,
                v.LastName,
                v.Suffix,
                v.Prefix,
                v.Designation,
                v.Department,
                v.Organization,
                ValidIdBase64 = GetImageAsBase64(v.ValidIdPath),
            }))
            .ToList();

            var firstReg = registrationsWithThisCar.First();

            return Ok(new
            {
                Type = "Vehicle Pass",
                vehicle.VehiclePlateNo,
                vehicle.VehicleModel,
                vehicle.VehicleColor,
                // Driver Details
                Prefix = vehicle.Prefix,
                DriverFirstName = vehicle.FirstName,
                DriverMiddleName = vehicle.MiddleName,
                DriverLastName = vehicle.LastName,
                Suffix = vehicle.Suffix,
                DriverIdBase64 = GetImageAsBase64(vehicle.ValidIdPath),

                // Organization & Visit Info
                Organization = firstReg.OrgName,
                VisitDateRaw = firstReg.VisitDateTime, // Full DateTime object
                VisitDateFormatted = firstReg.VisitDateTime.ToString("MMMM dd, yyyy"),

                // Branch Details
                BranchName = firstReg.Branch != null ? firstReg.Branch.BranchName : "N/A",
                Location = firstReg.Branch != null ? firstReg.Branch.Location : "N/A",

                // Passengers
                Passengers = passengers
            });
        }

        [HttpPatch("admin/batch-approve")]
        public async Task<IActionResult> BatchApprove([FromBody] List<int> registrationIds)
        {
            if (registrationIds == null || !registrationIds.Any())
                return BadRequest(new { message = "No IDs provided." });

            // Ensure we Include everything required for the PDF
            var registrations = await _context.Registrations
                .Include(r => r.Branch)
                .Include(r => r.Visitors)
                .Include(r => r.Vehicles)
                .Where(r => registrationIds.Contains(r.Id))
                .ToListAsync();

            try
            {
                foreach (var reg in registrations)
                {
                    // SAFETY CHECK: Ensure the QR data isn't empty
                    if (string.IsNullOrEmpty(reg.RegistrationCode))
                    {
                        // Regenerate if missing for some reason
                        reg.RegistrationCode = GenerateCode("REG");
                    }

                    if (reg.IsApproved != true)
                    {
                        reg.IsApproved = true;

                        // Call the email service
                        SendRegistrationEmail(reg);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Successfully processed {registrations.Count} records." });
            }
            catch (Exception ex)
            {
                // This will now catch exactly which part of the PDF/Email failed
                return StatusCode(500, new { error = $"Batch process failed: {ex.Message}" });
            }
        }

        [HttpGet("admin/pending-approvals")]
        public async Task<IActionResult> GetPendingApprovals([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            // 1. Ensure page parameters are valid
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Registrations
                .Include(r => r.Branch)
                .Include(r => r.Visitors)
                .Include(r => r.Vehicles)
                .Where(r => r.IsApproved == null);

            // 2. Get total count for the frontend pagination controls
            var totalRecords = await query.CountAsync();

            // 3. STEP ONE: Get the records from the Database first
            var pagedRegistrations = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(); // This executes the SQL and brings data into RAM

            // 4. STEP TWO: Perform the projection in memory (AsEnumerable)
            var pending = pagedRegistrations.Select(r => new
            {
                r.Id,
                r.RegistrationCode,
                OrgName = r.Visitors.FirstOrDefault()?.Organization ?? "N/A",
                r.PurposeOfVisit,
                r.PersonToVisit,
                r.VisitDateTime,
                r.CreatedAt,
                VisitorName = r.Visitors.Select(v =>
                    (v.Prefix + " " + v.FirstName + " " + (v.MiddleName ?? "") + " " + v.LastName + " " + v.Suffix)
                    .Replace("  ", " ").Trim()).FirstOrDefault(),
                VisitorEmail = r.Visitors.FirstOrDefault()?.Email,
                VisitorIdBase64 = GetImageAsBase64(r.Visitors.FirstOrDefault()?.ValidIdPath),

                // --- ADD THIS SECTION ---
                DriverName = r.Vehicles.Select(v =>
                    ((v.Prefix ?? "") + " " + v.FirstName + " " + (v.MiddleName ?? "") + " " + v.LastName + (v.MiddleName ?? ""))
                    .Replace("  ", " ").Trim()).FirstOrDefault() ?? "N/A",
                // -------------------------

                Branch = r.Branch?.BranchName ?? "N/A",
                Location = r.Branch?.Location ?? "N/A"
            }).ToList();

            return Ok(new
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = pending
            });
        }


        [HttpGet("admin/approved-visitors")]
        public async Task<IActionResult> GetApproved([FromQuery] DateTime? visitDate, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;

                var query = _context.Registrations
                    .Include(r => r.Branch)
                    .Include(r => r.Visitors)
                    .Include(r => r.Vehicles)
                    .Where(r => r.IsApproved == true);

                if (visitDate.HasValue)
                {
                    var targetDate = visitDate.Value.Date;
                    query = query.Where(r => r.VisitDateTime.Date == targetDate);
                }

                var totalRecords = await query.CountAsync();

                // 1. Fetch the raw data from SQL into memory
                var dbList = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 2. Perform mapping/Base64 conversion in memory
                var approvedList = dbList.Select(r => new
                {
                    r.Id,
                    r.RegistrationCode,
                    OrgName = r.Visitors.FirstOrDefault()?.Organization ?? "N/A",
                    r.PurposeOfVisit,
                    r.PersonToVisit,
                    r.VisitDateTime,
                    r.CreatedAt,
                    VisitorName = r.Visitors.Select(v =>
                        (v.Prefix + " " + v.FirstName + " " + (v.MiddleName ?? "") + " " + v.LastName + " " + v.Suffix)
                        .Replace("  ", " ").Trim()).FirstOrDefault(),
                    VisitorEmail = r.Visitors.FirstOrDefault()?.Email ?? "N/A",

                    // Call your custom method here
                    VisitorIdBase64 = GetImageAsBase64(r.Visitors.FirstOrDefault()?.ValidIdPath),

                    // --- ADD THIS SECTION ---
                    DriverName = r.Vehicles.Select(v =>
                        (v.FirstName + " " + (v.MiddleName ?? "") + " " + v.LastName)
                        .Replace("  ", " ").Trim()).FirstOrDefault() ?? "No Vehicle",
                    PlateNumber = r.Vehicles.FirstOrDefault()?.VehiclePlateNo ?? "N/A",
                    // -------------------------

                    Branch = r.Branch?.BranchName ?? "N/A",
                    Location = r.Branch?.Location ?? "N/A"
                }).ToList();

                return Ok(new
                {
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    PageNumber = pageNumber,
                    Data = approvedList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("export-approved-visitors-zip")]
        public async Task<IActionResult> ExportApprovedVisitorsZip([FromQuery] DateTime? visitDate)
        {
            try
            {
                var query = _context.Visitors
                    .Include(v => v.Registration)
                    .Where(v => v.Registration.IsApproved == true);

                if (visitDate.HasValue)
                {
                    query = query.Where(v => v.Registration.VisitDateTime.Date == visitDate.Value.Date);
                }

                var visitors = await query.ToListAsync();
                if (!visitors.Any()) return NotFound("No approved visitors found.");

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        var csvBuilder = new StringBuilder();
                        // Adding QRCodePath to the CSV for better tracking
                        csvBuilder.AppendLine("VisitorID,FullName,Position,Company,ProfilePicture");

                        foreach (var v in visitors)
                        {
                            var regCode = v.Registration.RegistrationCode;
                            string fullName = $"{v.Prefix} {v.FirstName} {v.MiddleName} {v.LastName} {v.Suffix}".Replace("  ", " ").Trim();

                            string extension = !string.IsNullOrEmpty(v.ValidIdPath) ? Path.GetExtension(v.ValidIdPath) : ".jpg";
                            string imageInsideZipPath = $"Visitor Folder/Images/{regCode}{extension}";
                            string qrInsideZipPath = $"Visitor Folder/QR/{regCode}_qr.jpg";

                            // Update CSV Row
                            csvBuilder.AppendLine($"\"{regCode}\",\"{fullName}\",\"{v.Designation}\",\"{v.Registration.OrgName}\",\"{regCode}{extension}\"");

                            // 1. Add Profile Image (From Disk)
                            if (!string.IsNullOrEmpty(v.ValidIdPath))
                            {
                                var fullPathOnDisk = Path.Combine(Directory.GetCurrentDirectory(), v.ValidIdPath.TrimStart('/', '\\'));
                                if (System.IO.File.Exists(fullPathOnDisk))
                                {
                                    archive.CreateEntryFromFile(fullPathOnDisk, imageInsideZipPath);
                                }
                            }

                            // 2. Add QR Code (GENERATED ON THE FLY)
                            // This uses your existing GenerateQrBytes method
                            byte[] qrBytes = GenerateQrBytes(regCode);
                            var qrEntry = archive.CreateEntry(qrInsideZipPath);
                            using (var entryStream = qrEntry.Open())
                            {
                                await entryStream.WriteAsync(qrBytes, 0, qrBytes.Length);
                            }
                        }

                        // 3. Add the CSV
                        var csvEntry = archive.CreateEntry("Visitor Folder/Visitor.csv");
                        using (var entryStream = csvEntry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            await writer.WriteAsync(csvBuilder.ToString());
                        }
                    }

                    memoryStream.Position = 0;
                    string zipName = $"NPOPSSIC_Export_{DateTime.Now:yyyyMMdd}.zip";
                    return File(memoryStream.ToArray(), "application/zip", zipName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Export failed: {ex.Message}" });
            }
        }


        //VISIT STATUS
        // 0 - Pending
        //1 - Guard Entry / checked in
        [HttpPost("confirm-entry")]
        public async Task<IActionResult> ConfirmEntry([FromBody] EntryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RegistrationCode))
                return BadRequest(new { message = "Registration code is required." });

            // 1. Find the registration that is already approved
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.RegistrationCode == request.RegistrationCode && r.IsApproved == true);

            if (registration == null)
            {
                return NotFound(new { message = "Valid approved registration not found for this code." });
            }

            // 2. Prevent duplicate check-ins
            if (registration.VisitStatus == 1)
            {
                return BadRequest(new { message = "This visitor has already been checked in." });
            }

            try
            {
                // 3. Set status to 1 (Checked In)
                registration.VisitStatus = 1;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Entry confirmed successfully!",
                    visitorName = registration.OrgName,
                    entryTime = DateTime.Now.ToString("hh:mm tt")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to confirm entry: {ex.Message}" });
            }
        }

        //century gothic
        private void SendRegistrationEmail(Registration reg)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            byte[] pdfBytes = GenerateRegistrationPdf(reg);

            // 1. Format Visitor Full Name
            var firstVisitor = reg.Visitors.FirstOrDefault();
            string firstName = firstVisitor?.FirstName ?? "Visitor";
            string middleName = !string.IsNullOrWhiteSpace(firstVisitor?.MiddleName)
                                ? firstVisitor.MiddleName + " "
                                : "";
            string lastName = firstVisitor?.LastName ?? "";
            string fullVisitorName = $"{firstName} {middleName}{lastName}".Trim();

            // 2. Determine if Vehicle Pass exists
            bool hasVehicle = reg.Vehicles != null && reg.Vehicles.Any();
            string passType = hasVehicle ? "Visitor Pass and Vehicle Pass" : "Visitor Pass";

            // 3. Format Details
            string visitorOrganization = firstVisitor?.Organization ?? "N/A";
            string siteLocation = $"{reg.Branch?.BranchName} / {reg.Branch?.Location}";
            string visitDate = reg.VisitDateTime.ToString("MMMM dd, yyyy 'at' hh:mm tt");

            _emailQueue.QueueEmail(new EmailMessage
            {
                To = firstVisitor?.Email ?? "",
                Subject = $"{passType} – {fullVisitorName}",
                Body = $@"
        <div style='font-family: ""Century Gothic"", AppleGothic, sans-serif; line-height: 1.6; color: #333; max-width: 600px;'>
            <h3 style='color: #2c3e50;'>{passType} Approved</h3>
            <p>Dear <strong>{fullVisitorName}</strong>,</p>
            
            <p>We are pleased to inform you that your <strong>{passType}</strong> has been approved.</p>
            
            <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; border-left: 4px solid #2c3e50; margin: 20px 0;'>
                <p style='margin: 5px 0;'><strong>Site:</strong> {siteLocation}</p>
                <p style='margin: 5px 0;'><strong>Visit Schedule:</strong> {visitDate}</p>
                <p style='margin: 5px 0;'><strong>Visitor:</strong> {fullVisitorName}</p>
                <p style='margin: 5px 0;'><strong>Company:</strong> {visitorOrganization}</p>
                <p style='margin: 5px 0;'><strong>Visitee:</strong> {reg.PersonToVisit}</p>
            </div>

            <p><strong>Instructions:</strong><br />
            To facilitate an orderly and seamless entry process, compliance with the following requirements is mandatory:</p>
            <ul style='padding-left: 20px;'>
                <li style='margin-bottom: 10px;'><strong>Visitor Pass:</strong> You are required to present your Visitor Pass (attached in the PDF) to the Registration Officer.</li>
                <li style='margin-bottom: 10px;'><strong>Identification:</strong> Please bring your uploaded valid government-issued ID for verification purposes.</li>
                <li style='margin-bottom: 10px;'><strong>Vehicle Entry:</strong>  If you are arriving in a vehicle, ensure the Vehicle Pass QR code is ready for scanning along with the uploaded valid government-issued IDs of the driver and passengers.</li>            
            </ul>
            
            <p>If you need to reschedule or have any questions, please contact <a href='mailto:npicc@npo.gov.ph' style='color: #3498db;'>npicc@npo.gov.ph</a>.</p>
            
            <p style='margin-top: 30px;'>Best regards,<br />
            <strong>NPO-PSSIC Security Team</strong></p>
            
            <hr style='border: 0; border-top: 1px solid #eee; margin-top: 20px;' />
            <small style='color: #777;'>This is an automated message. Please do not reply directly to this email.</small>
        </div>",
                AttachmentBytes = pdfBytes,
                AttachmentName = $"{(hasVehicle ? "Visitor_and_Vehicle_Pass" : "Visitor_Pass")}_{fullVisitorName.Replace(" ", "_")}.pdf"
            });
        }

        private byte[] GenerateRegistrationPdf(Registration reg)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                // --- PAGE 1: MAIN ENTRY / GROUP PASS ---
                container.Page(page =>
                {
                    page.Margin(1, Unit.Inch);
                    page.Size(PageSizes.A4);

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(150);
                        col.Item().AlignCenter().Text("VISITOR PASS")
                            .FontSize(50).Bold();
                        col.Item().AlignCenter().Text($"{reg.Branch?.Location}").FontSize(12).SemiBold();
                        col.Item().AlignCenter().Text($"{reg.VisitDateTime:f}").FontSize(14);
                        // Centered QR Code
                        col.Item().AlignCenter().Width(400).Image(GenerateQrBytes(reg.RegistrationCode));

                        col.Item().PaddingTop(20).AlignCenter().Text("Present this at the Registration Area along with your\r\nuploaded valid ID")
                            .FontSize(12).Italic();
                    });
                });

                // --- SUBSEQUENT PAGES: ONE PAGE PER VEHICLE ---
                foreach (var car in reg.Vehicles)
                {
                    var driverFullName = $"{car.FirstName} {car.MiddleName} {car.LastName}".Replace("  ", " ").Trim();
                    container.Page(page =>
                    {
                        page.Margin(1, Unit.Inch);
                        page.Size(PageSizes.A4);

                        page.Content().Column(col =>
                        {
                            col.Item().PaddingTop(150);
                            col.Item().AlignCenter().Text("VEHICLE PASS")
                                .FontSize(50).Bold();
                            col.Item().AlignCenter().Text($"{reg.Branch?.Location}").FontSize(12).SemiBold();
                            col.Item().AlignCenter().Text($"{reg.VisitDateTime:f}").FontSize(14);

                            // Centered Vehicle QR
                            col.Item().AlignCenter().Width(400).Image(GenerateQrBytes(car.VehicleQrCode));

                            col.Item().PaddingTop(20).AlignCenter().Text("Present this Vehicle Pass along with the uploaded valid IDs\r\nof the passengers at the Main Gate")
                                .FontSize(12).Italic();
                        });
                    });
                }
            }).GeneratePdf();
        }

        private byte[] GenerateQrBytes(string content)
        {
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions { Width = 500, Height = 500, Margin = 0 }
            };
            using var img = writer.Write(content);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }

        private string GenerateCode(string prefix) =>
            $"{prefix}-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";



        private string GetImageAsBase64(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return string.Empty;

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);

            if (!System.IO.File.Exists(fullPath)) return string.Empty;

            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            string base64String = Convert.ToBase64String(fileBytes); // Simplified: Convert.ToBase64String(fileBytes)

            string extension = Path.GetExtension(fullPath).ToLower().Replace(".", "");

            // Determine the correct MIME type
            string mimeType = extension switch
            {
                "pdf" => "application/pdf",
                "png" => "image/png",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                _ => "image/jpeg" // Default to jpeg for jpg/jpeg and others
            };

            return $"data:{mimeType};base64,{Convert.ToBase64String(fileBytes)}";
        }

        private async Task<string> SaveBase64Image(string base64Data, string subFolder)
        {
            if (string.IsNullOrWhiteSpace(base64Data))
                return string.Empty;

            try
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", subFolder);
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string rawData = base64Data;
                string extension = "jpg"; // Default extension

                if (base64Data.Contains(","))
                {
                    var parts = base64Data.Split(',');
                    string mimeType = parts[0].ToLower();

                    // Detect extension based on MIME type
                    if (mimeType.Contains("png")) extension = "png";
                    else if (mimeType.Contains("pdf")) extension = "pdf"; // Added PDF support
                    else if (mimeType.Contains("jpeg") || mimeType.Contains("jpg")) extension = "jpg";

                    rawData = parts[1];
                }

                byte[] fileBytes = Convert.FromBase64String(rawData);

                string fileName = $"{Guid.NewGuid()}.{extension}";
                string fullPath = Path.Combine(folderPath, fileName);

                await System.IO.File.WriteAllBytesAsync(fullPath, fileBytes);

                // Return path relative to project for DB storage
                return Path.Combine("wwwroot", "uploads", subFolder, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File Save Error: {ex.Message}");
                return string.Empty;
            }
        }

        public class EntryRequest
        {
            public string RegistrationCode { get; set; }
        }
    }
}