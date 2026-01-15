using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class ExamListResponse
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = "";
        public string AcademicYear { get; set; } = "";
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = "Draft";
        public List<ExamClassDto> Classes { get; set; } = new();
    }

    public class ExamClassDto
    {
        public int ClassId { get; set; }
    }

}
