using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class RegisterTeacherRequest
    {
        public int SchoolId { get; set; }

        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }

        public DateOnly JoiningDate { get; set; }
        public string? Qualification { get; set; }
    }


}
