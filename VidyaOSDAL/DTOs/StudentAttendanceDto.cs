using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class StudentAttendanceResponse
    {
        public int TotalDays { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public List<StudentAttendanceRecordDto> Records { get; set; } = new();
    }

    public class StudentAttendanceRecordDto
    {
        public DateOnly Date { get; set; }
        public string Status { get; set; } = ""; // "Present", "Absent", etc.
    }
}
