using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class LeaveResponse
    {
        public int LeaveId { get; set; }
        public string Status { get; set; } = "";
        public DateOnly AppliedAt { get; set; }
    }
}
