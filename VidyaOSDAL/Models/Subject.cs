using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Subject
{
    public int SubjectId { get; set; }

    public int SchoolId { get; set; }

    public int ClassId { get; set; }

    public string SubjectName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<ExamSubject> ExamSubjects { get; set; } = new List<ExamSubject>();
}
