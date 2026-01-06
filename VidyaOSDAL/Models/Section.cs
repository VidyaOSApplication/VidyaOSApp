using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Section
{
    public int SectionId { get; set; }

    public int? SchoolId { get; set; }

    public int? ClassId { get; set; }

    public string? SectionName { get; set; }

    public int? RollSeq { get; set; }

    public bool? IsActive { get; set; }
}
