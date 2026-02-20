using System.Text.Json.Serialization;

public class RegistrationDTO
{
    // Required fields (No ?)
    public string OrgName { get; set; } = string.Empty;
    public DateTime VisitDateTime { get; set; }
    public int BranchId { get; set; }
    public string? PersonToVisit { get; set; }
    public string? Department_PersonToVisit { get; set; }
    public string? PurposeOfVisit { get; set; }

    // --- ADD THESE REGISTRANT FIELDS ---
    public string RegistrantFirstName { get; set; } = string.Empty;
    public string? RegistrantMiddleName { get; set; }
    public string RegistrantLastName { get; set; } = string.Empty;
    public string RegistrantEmail { get; set; } = string.Empty;
    public string? RegistrantDesignation { get; set; }
    public string? RegistrantOrganization { get; set; }
    public string RegistrantPrefix { get; set; } = string.Empty;
    public string RegistrantSuffix { get; set; } = string.Empty;

    public string? RegistrantValidID { get; set; }
    // ------------------------------------

    // Collections should be initialized, not nullable, to avoid null reference exceptions
    public List<VisitorDTO> Visitors { get; set; } = new();
    public List<VehicleDTO> Vehicles { get; set; } = new();
}

public class VisitorDTO
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string? Designation { get; set; } // Optional
    public string? Department { get; set; }  // Optional depending on form rules
    public string Email { get; set; } = string.Empty;
    public string? ValidId { get; set; }     // Optional if they upload later
    public string? Organization { get; set; }
}

public class VehicleDTO
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }

    [JsonPropertyName("validIdPath")] // Ensure this matches your Vue payload key
    public string? ValidIdPath { get; set; }
    public string? PlateNo { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
}

public class RegistrantDTO
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
}

