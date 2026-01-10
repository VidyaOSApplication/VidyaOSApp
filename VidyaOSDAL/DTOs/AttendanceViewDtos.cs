using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AttendanceViewStudentDto
    {
        public int RollNo { get; set; }
        public string AdmissionNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Status { get; set; } = ""; // Present | Absent | Leave | NotMarked
    }

    public class AttendanceViewResponse
    {
        public DateOnly AttendanceDate { get; set; }
        public bool AttendanceTaken { get; set; }

        public AttendanceSummary Summary { get; set; } = new();
        public List<AttendanceViewStudentDto> Students { get; set; } = new();
    }

    public class AttendanceSummary
    {
        public int Total { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Leave { get; set; }
        public int NotMarked { get; set; }
    }

}
