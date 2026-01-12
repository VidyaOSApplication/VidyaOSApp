using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class CollectFeeRequest
    {
        public int StudentId { get; set; }
        public string FeeMonth { get; set; } = "";   // "2026-03"
        public string PaymentMode { get; set; } = "Cash";
    }

}
