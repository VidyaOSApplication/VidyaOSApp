using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.Models
{
    public class PendingFeeResponse
    {
        public int StudentFeeId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string AdmissionNo { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string FeeMonth { get; set; } = "";
        public decimal Amount { get; set; }
    }

}
