using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AttendanceStudentDto
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public int RollNo { get; set; }
        public string AdmissionNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Status { get; set; } = "Absent"; // Present | Absent | Leave
        public bool IsEditable { get; set; }
    }

    public class AttendanceFetchResponse
    {
        public DateOnly AttendanceDate { get; set; }
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public List<AttendanceStudentDto> Students { get; set; } = new();
    }

    public class AttendanceMarkRequest
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public DateOnly AttendanceDate { get; set; }
        public int MarkedByUserId { get; set; }
        public List<AttendanceRecordDto> Records { get; set; } = new();
    }

    public class AttendanceRecordDto
    {
        public int UserId { get; set; }
        public string Status { get; set; } = ""; // Present | Absent
    }

}
