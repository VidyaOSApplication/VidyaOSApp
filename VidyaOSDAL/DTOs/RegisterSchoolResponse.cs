using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class RegisterSchoolResponse
    {
        // School info
        public int SchoolId { get; set; }
        public string SchoolName { get; set; } = "";
        public string SchoolCode { get; set; } = "";

        // Admin user info
        public int AdminUserId { get; set; }
        public string AdminUsername { get; set; } = "";

        // Meta
        public DateTime CreatedAt { get; set; }
    }

}
