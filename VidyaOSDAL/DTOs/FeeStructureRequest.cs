using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class FeeStructureRequest
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int? StreamId { get; set; }
        public string FeeName { get; set; } = "";
        public decimal MonthlyAmount { get; set; }
    }

}
