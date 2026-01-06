using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class NotificationLog
{
    public int NotificationId { get; set; }

    public int? SchoolId { get; set; }

    public int? UserId { get; set; }

    public string? Message { get; set; }

    public string? Channel { get; set; }

    public string? Status { get; set; }

    public DateTime? SentAt { get; set; }
}
