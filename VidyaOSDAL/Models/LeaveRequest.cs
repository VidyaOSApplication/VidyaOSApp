using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class LeaveRequest
{
    public int LeaveId { get; set; }

    public int? SchoolId { get; set; }

    public int? UserId { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateOnly? AppliedOn { get; set; }

    public int? ApprovedBy { get; set; }

    public DateOnly? ApprovedOn { get; set; }

}
