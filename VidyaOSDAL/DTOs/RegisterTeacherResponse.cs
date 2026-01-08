using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class RegisterTeacherResponse
    {
        public int TeacherId { get; set; }
        public int UserId { get; set; }

        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string TempPassword { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }

}
