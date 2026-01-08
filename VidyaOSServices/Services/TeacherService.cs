using Microsoft.EntityFrameworkCore;
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
    public class TeacherService
    {
        private readonly VidyaOsContext _context;
        private readonly TeacherHelper _teacherHelper;
        public TeacherService(VidyaOsContext context,TeacherHelper teacherHelper)
        {
            _context = context;
            _teacherHelper = teacherHelper;
        }

        public async Task<ApiResult<RegisterTeacherResponse>> RegisterTeacherAsync(
            RegisterTeacherRequest req)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // ---------- VALIDATIONS ----------
                if (req.SchoolId <= 0)
                    return ApiResult<RegisterTeacherResponse>.Fail("Invalid school.");

                var schoolExists = await _context.Schools
                    .AnyAsync(s => s.SchoolId == req.SchoolId && s.IsActive == true);

                if (!schoolExists)
                    return ApiResult<RegisterTeacherResponse>.Fail("School not found.");

                if (string.IsNullOrWhiteSpace(req.FullName))
                    return ApiResult<RegisterTeacherResponse>.Fail("Teacher name is required.");

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        req.Phone, @"^[6-9]\d{9}$"))
                    return ApiResult<RegisterTeacherResponse>.Fail("Invalid phone number.");

                // ---------- AUTO USERNAME ----------
                string username = await _teacherHelper.GenerateTeacherUsernameAsync(req.FullName);

                // ---------- TEMP PASSWORD ----------
                string tempPassword = _teacherHelper.GenerateTempPassword(req.FullName);

                // ---------- CREATE USER ----------
                var user = new User
                {
                    SchoolId = req.SchoolId,
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                    Role = "Teacher",
                    Phone = req.Phone,
                    Email = req.Email,
                    IsFirstLogin = true,   // 🔥 CRITICAL
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // ---------- CREATE TEACHER ----------
                var teacher = new Teacher
                {
                    SchoolId = req.SchoolId,
                    UserId = user.UserId, // 🔥 RELATION
                    FullName = req.FullName.Trim(),
                    Phone = req.Phone,
                    Email = req.Email,
                    JoiningDate = req.JoiningDate,
                    Qualification = req.Qualification,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return ApiResult<RegisterTeacherResponse>.Ok(
                    new RegisterTeacherResponse
                    {
                        TeacherId = teacher.TeacherId,
                        UserId = user.UserId,
                        FullName = teacher.FullName!,
                        Username = user.Username!,
                        TempPassword = tempPassword, // show once
                        CreatedAt = teacher.CreatedAt!.Value
                    },
                    "Teacher registered successfully."
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
