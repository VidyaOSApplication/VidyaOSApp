using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class GenerateMonthlyFeeResponse
    {
        public int TotalStudents { get; set; }
        public int FeesGenerated { get; set; }
        public int AlreadyExists { get; set; }
    }
}
