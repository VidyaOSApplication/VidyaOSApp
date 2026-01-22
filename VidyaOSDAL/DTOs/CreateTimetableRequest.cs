using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class CreateTimetableRequest
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int? SectionId { get; set; }

        public int SubjectId { get; set; }
        public int DayOfWeek { get; set; }   // 1 = Monday
        public int PeriodNo { get; set; }

        public string StartTime { get; set; } = ""; // "12:30"
        public string EndTime { get; set; } = "";   // "13:30"

        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }

        public string AcademicYear { get; set; } = "";
    }

}
