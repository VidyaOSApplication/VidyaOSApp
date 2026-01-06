using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSServices.Services
{
    public class StudentService
    {
        private readonly VidyaOsContext _context;
        public StudentService(VidyaOsContext context)
        {
            _context = context;
        }

        public List<Student> GetAllStudents()
        {
            List<Student> students = new List<Student>();
            try
            {
                students = _context.Students.ToList();
                return students;
            }
            catch (Exception ex)
            {

                return students;
            }
        }
    }
}
