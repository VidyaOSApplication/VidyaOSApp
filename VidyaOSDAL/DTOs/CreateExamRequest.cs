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
        public string ExamName { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public List<int> ClassIds { get; set; } = new();
    }

}
