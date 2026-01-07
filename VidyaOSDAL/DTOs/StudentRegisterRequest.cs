using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class StudentRegisterRequest
    {
        public int SchoolId { get; set; }

        public string FirstName { get; set; } = "";
        public string Email { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Gender { get; set; } = "";
        public DateTime DOB { get; set; }

        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public string AcademicYear { get; set; } = "";

        public DateTime AdmissionDate { get; set; }

        public string FatherName { get; set; } = "";
        public string ParentPhone { get; set; } = "";

        public string AddressLine1 { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
    }


}
