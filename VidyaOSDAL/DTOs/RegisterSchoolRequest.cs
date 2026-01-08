using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class RegisterSchoolRequest
    {
        // School
        public string SchoolName { get; set; } = null!;
        public string SchoolCode { get; set; } = null!;
        public string RegistrationNumber { get; set; } = null!;
        public int? YearOfFoundation { get; set; }
        public string BoardType { get; set; } = null!;
        public string? AffiliationNumber { get; set; }
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string AddressLine1 { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string? Pincode { get; set; }

        // Admin
        public string AdminUsername { get; set; } = null!;
        public string AdminPassword { get; set; } = null!;
    }

}
