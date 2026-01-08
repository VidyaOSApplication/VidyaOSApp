using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using VidyaOSHelper.SchoolHelper;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;

namespace VidyaOSServices.Services
{
    public class SchoolService
    {
        private readonly VidyaOsContext _context;
        private readonly VidyaOSHelper.SchoolHelper.SchoolHelper _schoolHelper;
        public SchoolService(VidyaOsContext context,SchoolHelper schoolHelper)
        {
            _context = context;
            _schoolHelper = schoolHelper;
        }
        public async Task<ApiResult<RegisterSchoolResponse>> RegisterSchoolAsync(
    RegisterSchoolRequest req)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // ---------- VALIDATION ----------
                if (string.IsNullOrWhiteSpace(req.SchoolName))
                    return ApiResult<RegisterSchoolResponse>.Fail("School name is required.");

                if (string.IsNullOrWhiteSpace(req.SchoolCode))
                    return ApiResult<RegisterSchoolResponse>.Fail("School code is required.");

                if (string.IsNullOrWhiteSpace(req.AdminUsername))
                    return ApiResult<RegisterSchoolResponse>.Fail("Admin username is required.");

                if (string.IsNullOrWhiteSpace(req.AdminPassword))
                    return ApiResult<RegisterSchoolResponse>.Fail("Admin password is required.");

                if (!Regex.IsMatch(req.Phone, @"^[6-9]\d{9}$"))
                    return ApiResult<RegisterSchoolResponse>.Fail("Invalid phone number.");

                // ---------- DUPLICATE CHECKS ----------
                if (await _context.Schools.AnyAsync(s => s.SchoolCode == req.SchoolCode))
                    return ApiResult<RegisterSchoolResponse>.Fail("School code already exists.");

                if (await _context.Users.AnyAsync(u => u.Username == req.AdminUsername))
                    return ApiResult<RegisterSchoolResponse>.Fail("Admin username already exists.");

                // ---------- CREATE SCHOOL ----------
                var school = new School
                {
                    SchoolName = req.SchoolName.Trim(),
                    SchoolCode = req.SchoolCode.Trim().ToUpper(),
                    RegistrationNumber = req.RegistrationNumber,
                    YearOfFoundation = req.YearOfFoundation,
                    BoardType = req.BoardType,
                    AffiliationNumber = req.AffiliationNumber,
                    Email = req.Email,
                    Phone = req.Phone,
                    AddressLine1 = req.AddressLine1,
                    City = req.City,
                    State = req.State,
                    Pincode = req.Pincode,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Schools.Add(school);
                await _context.SaveChangesAsync();

                // ---------- CREATE ADMIN USER ----------
                var adminUser = new User
                {
                    SchoolId = school.SchoolId, // 🔥 RELATION
                    Username = req.AdminUsername.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.AdminPassword),
                    Role = "SchoolAdmin",
                    Email = req.Email,
                    Phone = req.Phone,
                    IsFirstLogin = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return ApiResult<RegisterSchoolResponse>.Ok(
                    new RegisterSchoolResponse
                    {
                        SchoolId = school.SchoolId,
                        SchoolName = school.SchoolName!,      // ✅ FIX
                        SchoolCode = school.SchoolCode!,
                        AdminUserId = adminUser.UserId,
                        AdminUsername = adminUser.Username!,
                        CreatedAt = school.CreatedAt!.Value
                    },
                    "School registered successfully."
                );
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

    }
}
