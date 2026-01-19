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
        public string FeeMonth { get; set; } = string.Empty;
    }
}
