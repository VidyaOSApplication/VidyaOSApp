using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class SchoolProfileDto
    {
        public int SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public string? SchoolCode { get; set; }
        public string? RegistrationNumber { get; set; }
        public int? YearOfFoundation { get; set; }
        public string? BoardType { get; set; }
        public string? AffiliationNumber { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
    }
}
