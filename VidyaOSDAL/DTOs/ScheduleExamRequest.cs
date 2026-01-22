using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class ScheduleExamRequest
    {
        public int ExamId { get; set; }
        public int ClassId { get; set; }
        public int SchoolId { get; set; }
        public List<ScheduleSubjectDto> Subjects { get; set; } = [];
    }


    public class ScheduleSubjectDto
    {
        public int SubjectId { get; set; }
        public DateTime? ExamDate { get; set; }
        public int? MaxMarks { get; set; }
    }

}
