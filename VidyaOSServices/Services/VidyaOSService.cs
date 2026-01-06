using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSServices.Services
{
    public class VidyaOSService
    {
        private readonly VidyaOsContext _context;
        public VidyaOSService(VidyaOsContext context)
        {
            _context = context;
        }
    }
}
