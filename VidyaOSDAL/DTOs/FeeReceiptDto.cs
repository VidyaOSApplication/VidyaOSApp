using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class FeeReceiptDto
    {
        public int StudentFeeId { get; set; }
        public string ReceiptNo { get; set; } = null!;
        public string FeeMonth { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string PaymentMode { get; set; } = null!;
        public DateOnly? PaidOn { get; set; }
        public DateTime GenerationDateTime { get; set; } // 🚀 For IST Time display

        // School Info
        public string SchoolName { get; set; } = null!;
        public string? SchoolCode { get; set; }
        public string? AffiliationNo { get; set; }
        public string? SchoolAddress { get; set; }
        public string? SchoolEmail { get; set; }
        public string? SchoolPhone { get; set; }

        // Student Info
        public string StudentName { get; set; } = null!;
        public string AdmissionNo { get; set; } = null!;
        public string? RollNo { get; set; }
        public string ClassName { get; set; } = null!;
        public string SectionName { get; set; } = null!;
        public string? AcademicYear { get; set; }
    }
}
