using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class SaveStudentMarksRequest
    {
        public int SchoolId { get; set; }
        public int ExamId { get; set; }
        public int ClassId { get; set; }

        public List<StudentMarkItem> Marks { get; set; } = new();
    }

    public class StudentMarkItem
    {
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public int MarksObtained { get; set; }
        public bool IsAbsent { get; set; }
    }

}
