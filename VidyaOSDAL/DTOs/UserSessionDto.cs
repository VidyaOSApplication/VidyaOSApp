using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class UserSessionDto
    {
        // Identity
        public int UserId { get; set; }
        public string Role { get; set; } = null!;
        public string RegistrationNo { get; set; } = null!;
        public int SchoolId { get; set; }


        // School Details (From School Table)
        public string SchoolName { get; set; } = null!;
        public string SchoolCode { get; set; } = null!;
        public string AffiliationNo { get; set; } = null!;
        public string BoardType { get; set; } = null!;

        // Person Details (From Teacher or Student Table)
        public int? ProfileId { get; set; } // TeacherId or StudentId
        public string FullName { get; set; } = null!;
        public string? AdmissionNo { get; set; } // Only for students
        public int? RollNo { get; set; } // Only for students
    }
}
