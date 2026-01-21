using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AssignSubjectsRequest
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int? StreamId { get; set; }   // only for 11–12
        public List<int> MasterSubjectIds { get; set; } = new();
    }
}
