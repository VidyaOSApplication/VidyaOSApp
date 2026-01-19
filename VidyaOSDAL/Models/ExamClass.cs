using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class ExamClass
{
    public int ExamClassId { get; set; }

    public int ExamId { get; set; }

    public int ClassId { get; set; }
    public string Status { get; set; } = "Draft"; // ✅ ADD THIS

    public virtual Exam Exam { get; set; } = null!;
}
