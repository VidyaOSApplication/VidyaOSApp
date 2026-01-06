using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class StudyMaterial
{
    public int StudyMaterialId { get; set; }

    public int? SchoolId { get; set; }

    public int? ClassId { get; set; }

    public int? SubjectId { get; set; }

    public int? TeacherId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? MaterialType { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? UploadedOn { get; set; }

    public bool? IsActive { get; set; }
}
