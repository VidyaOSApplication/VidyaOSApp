using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class DashboardSummaryDto
    {
        public int StudentId { get; set; }
        public string SchoolName { get; set; } = null!;
        public string? Role { get; set; }

        // Admin counts
        public int? TotalStudents { get; set; }
        public int? TotalTeachers { get; set; }
        public double? AttendancePercentage { get; set; }

        // Individual details (for Student/Teacher)
        public string? FullName { get; set; }
        public string? AdmissionNo { get; set; } // Student only
        public int? RollNo { get; set; }         // Student only
        public SubscriptionSummaryDto? Subscription { get; set; }
    }
}
