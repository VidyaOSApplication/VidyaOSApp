using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class StudentMark
{
    public int StudentMarkId { get; set; }

    public int? SchoolId { get; set; }

    public int? StudentId { get; set; }

    public int? ExamId { get; set; }

    public int? SubjectId { get; set; }

    public int? MarksObtained { get; set; }

    public int? MaxMarks { get; set; }

    public DateTime? CreatedAt { get; set; }
}
