using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class ExamSubject
{
    public int ExamSubjectId { get; set; }

    public int? ExamId { get; set; }

    public int? SubjectId { get; set; }

    public int? MaxMarks { get; set; }
}
