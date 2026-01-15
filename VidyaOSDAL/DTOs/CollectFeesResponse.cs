using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class CollectFeesResponse
    {
        public string ReceiptNo { get; set; } = "";
        public int StudentId { get; set; }
        public List<string> PaidMonths { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public DateTime PaidOn { get; set; }
    }
}
