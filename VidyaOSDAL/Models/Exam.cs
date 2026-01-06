using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Exam
{
    public int ExamId { get; set; }

    public int? SchoolId { get; set; }

    public int? ClassId { get; set; }

    public string? ExamName { get; set; }

    public string? AcademicYear { get; set; }

    public DateOnly? ExamDate { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
