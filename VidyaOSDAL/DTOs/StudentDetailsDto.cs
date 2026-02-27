using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    namespace VidyaOSDAL.DTOs
    {
        public class StudentDetailsDto
        {
            public int StudentId { get; set; }
            public string FullName { get; set; } = null!;
            public string AdmissionNo { get; set; } = null!;
            public int? RollNo { get; set; }
            public string? AcademicYear { get; set; }

            // --- Academic Fields (IDs for Logic, Names for Display) ---
            public int? ClassId { get; set; }
            public string ClassName { get; set; } = null!;
            public int? SectionId { get; set; }
            public string SectionName { get; set; } = null!;
            public int? StreamId { get; set; }

            // --- Personal Info ---
            public string? Gender { get; set; }
            public DateTime? Dob { get; set; } // DateTime is used for easy JSON parsing in React Native

            // --- Contact & Address ---
            public string ParentPhone { get; set; } = null!;
            public string? FatherName { get; set; }
            public string? MotherName { get; set; }
            public string AddressLine1 { get; set; } = null!;
            public string City { get; set; } = null!;
            public string State { get; set; } = null!;
            public string SchoolName { get; set; } = null!;
        }
    }
}
