using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    [Keyless]
    public class StudentRegisterResponse
    {

        public int StudentId { get; set; }
        public string AdmissionNo { get; set; } = "";
        public string Username { get; set; } = "";
        public string TempPassword { get; set; } = "";
    }
}
