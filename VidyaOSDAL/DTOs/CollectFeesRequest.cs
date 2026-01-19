using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class CollectFeesRequest
    {
        public int SchoolId { get; set; }
        public int StudentId { get; set; }

        public int ClassId { get; set; }
        public int? StreamId { get; set; }


        // "2026-01", "2026-02"
        public List<string> FeeMonths { get; set; } = new();

        public string PaymentMode { get; set; } = "Cash";
    }
}
