using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Stream
{
    public int StreamId { get; set; }

    public int SchoolId { get; set; }

    public int ClassId { get; set; }

    public string StreamName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public int StreamMasterId { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual StreamMaster StreamMaster { get; set; } = null!;
}
