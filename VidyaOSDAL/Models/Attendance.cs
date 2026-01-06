using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int? SchoolId { get; set; }

    public int? UserId { get; set; }

    public DateOnly? AttendanceDate { get; set; }

    public string? Status { get; set; }

    public string? Source { get; set; }
}
