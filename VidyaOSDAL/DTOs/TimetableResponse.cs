using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class TimetableResponse
    {
        public int TimetableId { get; set; }
        public int DayOfWeek { get; set; }
        public int PeriodNo { get; set; }

        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";

        public string SubjectName { get; set; } = "";
    }

}
