using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class MasterSubjectResponseDto
    {
        public int MasterSubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int? StreamId { get; set; }
        public string? StreamName { get; set; } // For display in the UI
        public bool IsActive { get; set; }
    }
}
