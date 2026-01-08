using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class TeacherProfile : MeResponse
    {
        public int SchoolId { get; set; }
        public int TeacherId { get; set; }
        public string FullName { get; set; } = "";
    }
}
