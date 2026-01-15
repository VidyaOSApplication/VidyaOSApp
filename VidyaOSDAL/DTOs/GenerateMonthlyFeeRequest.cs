using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class GenerateMonthlyFeeRequest
    {
        public int SchoolId { get; set; }
        public int Month { get; set; }   // 1–12
        public int Year { get; set; }
    }
}
