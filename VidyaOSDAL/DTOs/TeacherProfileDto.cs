using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class TeacherProfileDto
    {
        public int TeacherId { get; set; }
        public int? SchoolId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateOnly? JoiningDate { get; set; }
        public string? Qualification { get; set; }
        public bool? IsActive { get; set; }
    }
}
