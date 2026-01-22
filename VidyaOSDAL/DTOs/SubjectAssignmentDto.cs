using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class SubjectAssignmentDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = "";
        public bool Assigned { get; set; }
    }
}
