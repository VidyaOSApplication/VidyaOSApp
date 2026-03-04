using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class StreamMaster
{
    public int Id { get; set; }

    public string StreamName { get; set; } = null!;

    public virtual ICollection<Stream> Streams { get; set; } = new List<Stream>();
}
