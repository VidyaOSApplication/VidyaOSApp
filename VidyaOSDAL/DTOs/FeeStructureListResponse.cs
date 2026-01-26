using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class FeeStructureListResponse
    {
        public int FeeStructureId { get; set; }
        public int ClassId { get; set; }
        public int? StreamId { get; set; }
        public string StreamName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string FeeName { get; set; } = "";
        public decimal MonthlyAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
