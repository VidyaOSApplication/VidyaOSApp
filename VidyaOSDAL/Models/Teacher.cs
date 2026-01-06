using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public int? SchoolId { get; set; }

    public int? UserId { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateOnly? JoiningDate { get; set; }

    public string? Qualification { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
