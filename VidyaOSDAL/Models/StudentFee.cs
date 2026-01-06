using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class StudentFee
{
    public int StudentFeeId { get; set; }

    public int? StudentId { get; set; }

    public string? FeeMonth { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMode { get; set; }

    public string? Status { get; set; }

    public DateOnly? PaidOn { get; set; }
}
