using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{

    public class StudentRegisterRequest
    {
        // IDs & Basic Context
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int? StreamId { get; set; } // Keep only this one
        public string AcademicYear { get; set; } = ""; // e.g., "2025-26"

        // Student Identity
        public string FirstName { get; set; } = "";
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? Category { get; set; } // 🚀 New Category Field (General, OBC, etc.)
        public DateTime DOB { get; set; }
        public string? Email { get; set; }

        // Family & Contact
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string ParentPhone { get; set; } = "";
        public DateTime AdmissionDate { get; set; }

        // Address
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
    }
}
