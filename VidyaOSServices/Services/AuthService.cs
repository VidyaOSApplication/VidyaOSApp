using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;

namespace VidyaOSServices.Services
{
    public class AuthService
    {
        private readonly VidyaOsContext _context;
        private readonly AuthHelper _authHelper;
        public AuthService(VidyaOsContext vidyaOsContext, AuthHelper authHelper)
        {
            _context = vidyaOsContext;
            _authHelper = authHelper;
        }

        public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest req)
        {
            string normalizedUsername = req.Username.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == normalizedUsername &&
                    u.IsActive == true);

            if (user == null)
                return ApiResult<LoginResponse>.Fail("Invalid username or password.");

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return ApiResult<LoginResponse>.Fail("Invalid username or password.");

            string token = _authHelper.GenerateJwt(user);

            return ApiResult<LoginResponse>.Ok(
                new LoginResponse
                {
                    Token = token,
                    Role = user.Role!,
                    ExpiresIn = 3600
                },
                "Logged in successfully."
            );
        }

        public async Task<ApiResult<UserSessionDto>> GetUserSessionAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return ApiResult<UserSessionDto>.Fail("User not found.");

            var school = await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.SchoolId == user.SchoolId);

            var session = new UserSessionDto
            {
                UserId = user.UserId,
                Role = user.Role,
                SchoolId = user.SchoolId ?? 0,
                SchoolName = school?.SchoolName ?? "",
                SchoolCode = school?.SchoolCode ?? "",
                RegistrationNo = school?.RegistrationNumber ?? "",
                AffiliationNo = school?.AffiliationNumber ?? ""
            };

            if (user.Role == "Teacher")
            {
                var teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == userId);
                session.FullName = teacher?.FullName ?? "";
                session.ProfileId = teacher?.TeacherId;
            }
            else if (user.Role == "Student")
            {
                var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
                session.FullName = $"{student?.FirstName} {student?.LastName}";
                session.ProfileId = student?.StudentId;
                session.AdmissionNo = student?.AdmissionNo;
                session.RollNo = student?.RollNo;
            }

            return ApiResult<UserSessionDto>.Ok(session);
        }
    }
}
