using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class SubscriptionSummaryDto
    {
        public string PlanName { get; set; }
        public DateOnly? EndDate { get; set; }
        public int MaxStudents { get; set; }
    }
}
