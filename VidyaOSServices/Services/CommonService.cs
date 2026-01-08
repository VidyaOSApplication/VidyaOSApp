using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;



namespace VidyaOSServices.Services
{
    public class CommonService
    {
        private readonly VidyaOsContext _context;
        public CommonService(VidyaOsContext context)
        {
            _context = context;
            
        }
        public async Task<object> GetMyProfileAsync(ClaimsPrincipal user)
        {
            if (user == null)
                throw new UnauthorizedAccessException("User context not available.");

            // ---------- USER ID ----------
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("UserId claim missing.");

            int userId = int.Parse(userIdClaim.Value);

            // ---------- ROLE ----------
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedAccessException("Role claim missing.");

            string role = roleClaim.Value;

            // ---------- SCHOOL ID ----------
            var schoolIdClaim = user.FindFirst("schoolId");
            if (schoolIdClaim == null)
                throw new UnauthorizedAccessException("SchoolId claim missing.");

            int schoolId = int.Parse(schoolIdClaim.Value);



            if (role == "SchoolAdmin")
            {
                var school = await _context.Schools
                    .FirstAsync(s => s.SchoolId == schoolId);

                return new
                {
                    userId,
                    role,
                    schoolId,
                    schoolName = school.SchoolName
                };
            }

            if (role == "Teacher")
            {
                var teacher = await _context.Teachers
                    .FirstAsync(t => t.UserId == userId);

                return new
                {
                    userId,
                    role,
                    schoolId,
                    teacherId = teacher.TeacherId,
                    fullName = teacher.FullName
                };
            }

            var student = await _context.Students
                .FirstAsync(s => s.UserId == userId);

            return new
            {
                userId,
                role,
                schoolId,
                studentId = student.StudentId,
                classId = student.ClassId,
                sectionId = student.SectionId,
                admissionNo = student.AdmissionNo
            };
        }

    }
}
