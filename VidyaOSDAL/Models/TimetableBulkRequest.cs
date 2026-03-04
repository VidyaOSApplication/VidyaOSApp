using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.Models
{
    public class TimetableBulkRequest
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int? StreamId { get; set; }
        public string AcademicYear { get; set; } = "2025-26";
        public List<TimetableEntryDto> Entries { get; set; } = new();
    }

    public class TimetableEntryDto
    {
        public int SubjectId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public int PeriodNo { get; set; }
        public string StartTime { get; set; } = "08:00";
        public string EndTime { get; set; } = "08:45";
    }
}
