using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class PendingLeaveDto
    {
        public int LeaveId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "";

        public string Name { get; set; } = "";

        public int? ClassId { get; set; }
        public int? SectionId { get; set; }

        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public DateOnly AppliedOn { get; set; }
    }



}
