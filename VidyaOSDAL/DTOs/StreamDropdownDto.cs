using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class StreamDropdownDto
    {
        public int StreamId { get; set; }     // PK from Streams table
        public string StreamName { get; set; } = string.Empty; // From StreamMaster table
    }
}
