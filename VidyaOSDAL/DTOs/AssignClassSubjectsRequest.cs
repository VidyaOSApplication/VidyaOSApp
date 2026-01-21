using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AssignClassSubjectsRequest
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int? StreamId { get; set; } // required for 11/12
        public List<int> SubjectIds { get; set; } = new();
    }
}
