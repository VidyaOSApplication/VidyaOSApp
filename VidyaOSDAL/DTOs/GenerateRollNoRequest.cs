using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class GenerateRollNoRequest
    {
        public int SchoolId { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int? StreamId { get; set; }
    }

}
