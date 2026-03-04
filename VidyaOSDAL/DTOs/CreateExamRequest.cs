using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class CreateExamRequest
    {
        public int SchoolId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; } // Using DateTime for easy JSON parsing
        public DateTime? EndDate { get; set; }
    }

}
