using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class ClassTimetable
{
    public int TimetableId { get; set; }

    public int SchoolId { get; set; }

    public int ClassId { get; set; }

    public int? SectionId { get; set; }

    public int SubjectId { get; set; }

    public int DayOfWeek { get; set; }

    public int PeriodNo { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string AcademicYear { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

}
