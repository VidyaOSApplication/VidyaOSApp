using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class SubscriptionPlan
{
    public int PlanId { get; set; }

    public string? PlanName { get; set; }

    public decimal? PriceMonthly { get; set; }

    public int? MaxStudents { get; set; }

    public bool? IsActive { get; set; }
}
