using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class Homework
{
    public int HomeworkId { get; set; }

    public int? SchoolId { get; set; }

    public int? ClassId { get; set; }

    public int? SectionId { get; set; }

    public int? SubjectId { get; set; }

    public int? TeacherId { get; set; }

    public string? HomeworkType { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? AssignedDate { get; set; }

    public DateOnly? DueDate { get; set; }
}
