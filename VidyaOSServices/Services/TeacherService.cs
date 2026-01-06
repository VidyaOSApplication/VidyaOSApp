using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSServices.Services
{
    public class TeacherService
    {
        private readonly VidyaOsContext _context;
        public TeacherService(VidyaOsContext context)
        {
            _context = context;
        }
    }
}
