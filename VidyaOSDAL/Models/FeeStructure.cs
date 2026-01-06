using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class FeeStructure
{
    public int FeeStructureId { get; set; }

    public int? SchoolId { get; set; }

    public int? ClassId { get; set; }

    public string? FeeName { get; set; }

    public decimal? MonthlyAmount { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
