using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class TimeTable
{
    public int TimeTableId { get; set; }

    public int? SchoolId { get; set; }

    public int? ClassId { get; set; }

    public int? SectionId { get; set; }

    public string? DayOfWeek { get; set; }

    public int? PeriodNo { get; set; }

    public int? SubjectId { get; set; }

    public int? TeacherId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}
