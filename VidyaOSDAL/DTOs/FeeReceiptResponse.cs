using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class FeeReceiptResponse
    {
        public string ReceiptNo { get; set; } = "";
        public DateTime ReceiptDate { get; set; }

        public string SchoolName { get; set; } = "";
        public string SchoolAddress { get; set; } = "";

        public string StudentName { get; set; } = "";
        public string AdmissionNo { get; set; } = "";
        public string ClassSection { get; set; } = "";

        public string FeeMonth { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = "";
        public string? RollNo { get; set; }
        public string? AcademicSession { get; set; }
    }
}
