using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class School
{
    public int SchoolId { get; set; }

    public string? SchoolName { get; set; }

    public string? SchoolCode { get; set; }

    public string? RegistrationNumber { get; set; }

    public int? YearOfFoundation { get; set; }

    public string? BoardType { get; set; }

    public string? AffiliationNumber { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Pincode { get; set; }

    public int? AdmissionSeq { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
