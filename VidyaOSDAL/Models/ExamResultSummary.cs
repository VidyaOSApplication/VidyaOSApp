using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class ExamResultSummary
{
    public int ResultId { get; set; }

    public int StudentId { get; set; }

    public int ExamId { get; set; }

    public int TotalMarks { get; set; }

    public int ObtainedMarks { get; set; }

    public decimal? Percentage { get; set; }

    public string? Grade { get; set; }

    public string? ResultStatus { get; set; }

    public DateTime? GeneratedAt { get; set; }
}
