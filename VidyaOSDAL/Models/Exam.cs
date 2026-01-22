using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Exam
{
    public int ExamId { get; set; }

    public int SchoolId { get; set; }

    public string ExamName { get; set; } = null!;

    public string AcademicYear { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();

    public virtual ICollection<ExamSubject> ExamSubjects { get; set; } = new List<ExamSubject>();
}
