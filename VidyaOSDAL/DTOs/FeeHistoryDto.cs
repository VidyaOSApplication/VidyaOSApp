using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class FeeHistoryDto
    {
        public int StudentFeeId { get; set; }
        public string FeeMonth { get; set; } = "";
        public decimal Amount { get; set; }
        public DateOnly? PaidOn { get; set; }
        public string PaymentMode { get; set; } = "";
        public string ReceiptNo { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
