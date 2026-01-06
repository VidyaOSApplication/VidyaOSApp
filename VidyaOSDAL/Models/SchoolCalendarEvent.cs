using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class SchoolCalendarEvent
{
    public int EventId { get; set; }

    public int? SchoolId { get; set; }

    public string? EventType { get; set; }

    public string? EventName { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? ClassId { get; set; }

    public int? SectionId { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
