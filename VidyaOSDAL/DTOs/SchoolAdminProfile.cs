using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class SchoolAdminProfile: MeResponse
    {
        public int SchoolId { get; set; }
        public string SchoolName { get; set; } = "";
    }
}
