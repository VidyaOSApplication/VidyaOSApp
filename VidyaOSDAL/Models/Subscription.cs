using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Subscription
{
    public int SubscriptionId { get; set; }

    public int? SchoolId { get; set; }

    public int? PlanId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool? IsTrial { get; set; }

    public bool? IsActive { get; set; }
}
