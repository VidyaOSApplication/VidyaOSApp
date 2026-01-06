using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public int? SchoolId { get; set; }

    public string? ClassName { get; set; }

    public bool? IsActive { get; set; }
}
