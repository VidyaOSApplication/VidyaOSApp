public class RegisterSchoolRequest
{
    // ---------- REQUIRED ----------
    public string SchoolName { get; set; } = null!;
    public string SchoolCode { get; set; } = null!;
    public string BoardType { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string AdminUsername { get; set; } = null!;
    public string AdminPassword { get; set; } = null!;

    // ---------- OPTIONAL ----------
    public string? RegistrationNumber { get; set; }
    public string? AffiliationNumber { get; set; }
    public int? YearOfFoundation { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
}
