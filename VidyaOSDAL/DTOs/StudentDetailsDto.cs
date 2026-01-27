using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class StudentDetailsDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = null!;
        public string AdmissionNo { get; set; } = null!;
        public int? RollNo { get; set; }
        public string ClassName { get; set; } = null!;
        public string SectionName { get; set; } = null!;
        public string? AcademicYear { get; set; }
        public string ParentPhone { get; set; } = null!;
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string AddressLine1 { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
    }
}
