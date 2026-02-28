using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int? SchoolId { get; set; }

    public int? UserId { get; set; }

    public string? AdmissionNo { get; set; }

    public int? RollNo { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Gender { get; set; }

    public DateOnly? Dob { get; set; }

    public int? ClassId { get; set; }

    public int? SectionId { get; set; }

    public string? AcademicYear { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public string? FatherName { get; set; }

    public string? MotherName { get; set; }

    public string? ParentPhone { get; set; }

    public string? AddressLine1 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? StudentStatus { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? StreamId { get; set; }

    public string? Category { get; set; }
}
