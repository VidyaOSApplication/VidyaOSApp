using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class LookUpDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class ExamSelectionDataDto
    {
        public List<LookUpDto> Exams { get; set; } = new();
        public List<LookUpDto> Classes { get; set; } = new();
        public List<LookUpDto> Subjects { get; set; } = new();
        public List<LookUpDto> Sections { get; set; } = new(); // New
        public List<LookUpDto> Streams { get; set; } = new();  // New
    }

    public class BulkMarksEntryDto
    {
        public int StudentId { get; set; }
        public int? RollNo { get; set; }
        public string FullName { get; set; } = null!;
        public string AdmissionNo { get; set; } = null!;
        public int? MarksObtained { get; set; }
        public int MaxMarks { get; set; }
    }

    public class BulkSaveRequest
    {
        public int SchoolId { get; set; }
        public int ExamId { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int SectionId { get; set; }
        public int? StreamId { get; set; }
        public List<BulkMarksEntryDto> Marks { get; set; } = new();
    }


}
