using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    namespace VidyaOS.Models.DTOs
    {
        // Individual Subject Record
        public class SubjectMarkDto
        {
            public string SubjectName { get; set; } = null!;
            public int Obtained { get; set; }
            public int Max { get; set; }
        }

        public class StudentResultSummaryDto
        {
            public string FullName { get; set; } = null!;
            public string RollNo { get; set; } = null!;
            public string AdmissionNo { get; set; } = null!;
            public string ExamName { get; set; } = null!;
            public string ClassName { get; set; } = null!;
            public List<SubjectMarkDto> Marks { get; set; } = new();
            public int TotalObtained { get; set; }
            public int TotalMax { get; set; }
            public double Percentage { get; set; }
        }
    }
}
