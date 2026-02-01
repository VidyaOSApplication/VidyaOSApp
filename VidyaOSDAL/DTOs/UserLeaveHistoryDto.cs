using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class UserLeaveHistoryDto
    {
        public int LeaveId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public DateOnly AppliedOn { get; set; }
        
    }
}
