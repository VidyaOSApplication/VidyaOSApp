using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class AdmissionYearSequence
{
    public int SchoolId { get; set; }

    public int AdmissionYear { get; set; }

    public int LastSeq { get; set; }
}
