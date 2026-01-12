using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.Models
{
    public class LeaveActionRequest
    {
        public int LeaveId { get; set; }
        public int AdminUserId { get; set; }
        public string Action { get; set; } = ""; // MUST be "Approve" or "Reject"
    }
}

