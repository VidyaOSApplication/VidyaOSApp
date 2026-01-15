using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class StudentListDto
    {
        public int StudentId { get; set; }
        public string AdmissionNo { get; set; } = "";
        public string FullName { get; set; } = "";
        public int RollNo { get; set; }
    }
}
