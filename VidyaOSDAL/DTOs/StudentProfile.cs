using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class StudentProfile : MeResponse
    {
        public int SchoolId { get; set; }
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public string AdmissionNo { get; set; } = "";
    }
}
