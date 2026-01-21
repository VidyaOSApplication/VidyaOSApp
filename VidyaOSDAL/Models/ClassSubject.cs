using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class ClassSubject
{
    public int ClassSubjectId { get; set; }

    public int SchoolId { get; set; }

    public int ClassId { get; set; }

    public int? StreamId { get; set; }

    public int SubjectId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
