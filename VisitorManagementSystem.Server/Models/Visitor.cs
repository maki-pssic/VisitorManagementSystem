using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem.Server.Models
{

    public class Registration
    {
        public int Id { get; set; }
        public string? RegistrationCode { get; set; } // QR Code 1 (Personal/Group)
        public string? OrgName { get; set; }
        public DateTime VisitDateTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? PersonToVisit { get; set; }
        public bool? IsApproved { get; set; }
        public string? PurposeOfVisit { get; set; }
        public string? Department_PersonToVisit { get; set; }
        // Add these two lines
        public int? BranchId { get; set; }
        public virtual Branch Branch { get; set; } = null!;

        public int? VisitStatus { get; set; }

        // Relationships
        public List<Visitor> Visitors { get; set; } = new();
        public List<VehiclePass> Vehicles { get; set; } = new();

        public int? RegistrantId { get; set; }
        [ForeignKey("RegistrantId")]
        public virtual Registrant? Registrant { get; set; }
    }

    public class Visitor
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; } // Foreign Key

        [ForeignKey("RegistrationId")]
        public virtual Registration Registration { get; set; } = null!;
        public string? FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? LastName { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; } 
        public string? Email { get; set; }
        public string? ValidIdPath { get; set; }

        public string? Organization { get; set; }
    }

    public class VehiclePass
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; } // Foreign Key

        // ADD THIS NAVIGATION PROPERTY
        [ForeignKey("RegistrationId")]
        public virtual Registration Registration { get; set; } = null!;
        public string? VehicleQrCode { get; set; } // QR Code 2 (Vehicle Pass)
        public string? FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? LastName { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }
        public string? ValidIdPath { get; set; }

        [Column("PlateNo")] // Matches your SQL screenshot
        public string? VehiclePlateNo { get; set; }

        [Column("Model")]   // Matches your SQL screenshot
        public string? VehicleModel { get; set; }

        [Column("Color")]   // Matches your SQL screenshot
        public string? VehicleColor { get; set; }
    }

    public class Branch
    {
        public int Id { get; set; }
        public string? BranchName { get; set; } = string.Empty;
        public string? Location { get; set; } = string.Empty;
    }


    public class Registrant
    {
        public int Id { get; set; }
        public string? FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? LastName { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? Organization { get; set; }
        public string? Email { get; set; }
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }
        public string? RegistrantValidID { get; set; }
    }

}
