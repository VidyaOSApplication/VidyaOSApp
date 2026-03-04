using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class MasterSubjectDto
    {
        public int? MasterSubjectId { get; set; } // Null for Create
        public int SchoolId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int? StreamId { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class AssignSubjectDto
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int? StreamId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
    }
}
