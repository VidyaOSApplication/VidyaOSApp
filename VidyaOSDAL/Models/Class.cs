using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Class
{
    public int SchoolId { get; set; }

    public int ClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual School School { get; set; } = null!;

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

    public virtual ICollection<Stream> Streams { get; set; } = new List<Stream>();
}
