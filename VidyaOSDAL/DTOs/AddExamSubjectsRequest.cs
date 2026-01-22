using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AddExamSubjectsRequest
    {
        public int ExamId { get; set; }
        public int ClassId { get; set; }

        public List<ExamSubjectItem> Subjects { get; set; } = new();
    }

    public class ExamSubjectItem
    {
        public int SubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public int MaxMarks { get; set; }
    }

}
