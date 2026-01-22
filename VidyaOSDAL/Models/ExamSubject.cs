using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class ExamSubject
{
    public int ExamSubjectId { get; set; }

    public int ExamId { get; set; }

    public int ClassId { get; set; }

    public int SubjectId { get; set; }

    public DateOnly ExamDate { get; set; }

    public int MaxMarks { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
