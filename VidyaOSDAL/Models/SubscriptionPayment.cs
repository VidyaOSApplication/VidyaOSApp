using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class SubscriptionPayment
{
    public int PaymentId { get; set; }

    public int? SchoolId { get; set; }

    public int? SubscriptionId { get; set; }

    public string? PaymentGateway { get; set; }

    public string? PaymentOrderId { get; set; }

    public string? PaymentTransactionId { get; set; }

    public decimal? Amount { get; set; }

    public string? Currency { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? BillingCycle { get; set; }

    public DateTime? CreatedAt { get; set; }
}
