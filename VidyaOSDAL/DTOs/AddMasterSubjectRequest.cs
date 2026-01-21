using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AddMasterSubjectRequest
    {
        public int SchoolId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
    }
}
