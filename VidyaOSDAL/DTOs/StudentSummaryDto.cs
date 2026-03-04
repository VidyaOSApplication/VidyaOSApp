using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class StudentSummaryDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = null!;
        public string AdmissionNo { get; set; } = null!;
        public string ClassName { get; set; } = null!;
    }
}
