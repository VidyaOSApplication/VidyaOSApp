using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AdminDashboardSummaryDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public SubscriptionSummaryDto? Subscription { get; set; }
    }
}
