using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class BirthdayPersonDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = ""; // Student / Teacher
        public DateOnly Dob { get; set; }
    }

}
