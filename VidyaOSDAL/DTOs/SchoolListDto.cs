using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class SchoolListDto
    {
        public int SchoolId { get; set; }
        public string SchoolName { get; set; }
        public string SchoolCode { get; set; }
        public bool IsActive { get; set; }
    }
}
