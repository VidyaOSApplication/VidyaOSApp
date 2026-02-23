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
            // 1. Normalize and find the user
            string normalizedUsername = req.Username.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == normalizedUsername &&
                    u.IsActive == true);

            if (user == null)
                return ApiResult<LoginResponse>.Fail("Invalid username or password.");

            // 2. Verify Password
            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return ApiResult<LoginResponse>.Fail("Invalid username or password.");

            // 🚀 3. SUBSCRIPTION GUARD
            // SuperAdmins are typically excluded from subscription blocks to prevent lockout.
            if (user.Role != "SuperAdmin")
            {
                // Get the current date in DateOnly format to match database types
                var today = DateOnly.FromDateTime(DateTime.Now);

                var activeSub = await _context.Subscriptions
                    .Where(s => s.SchoolId == user.SchoolId && s.IsActive == true)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();

                // Validate that a subscription exists and the EndDate is not in the past
                if (activeSub == null || activeSub.EndDate < today)
                {
                    return ApiResult<LoginResponse>.Fail("Your VidyaOS subscription has expired or is inactive. Please contact support.");
                }
            }

            // 4. Generate Token and Return Response
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
