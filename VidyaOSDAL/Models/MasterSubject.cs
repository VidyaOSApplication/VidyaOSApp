using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class MasterSubject
{
    public int MasterSubjectId { get; set; }

    public int SchoolId { get; set; }

    public string SubjectName { get; set; } = null!;

    public int? StreamId { get; set; }

    public bool? IsActive { get; set; }
}
