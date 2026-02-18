using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AdminDashboardSummaryDto
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public SubscriptionSummaryDto? Subscription { get; set; }
    }
}
