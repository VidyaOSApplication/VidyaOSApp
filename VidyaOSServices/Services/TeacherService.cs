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
        public TeacherService(VidyaOsContext context, TeacherHelper teacherHelper)
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

        public async Task<AttendanceFetchResponse> GetStudentsForAttendanceAsync(
    int schoolId,
    int classId,
    int sectionId,
    DateOnly date,
    int? streamId)
        {
            // 🔐 Stream required only for 11–12
            if ((classId == 11 || classId == 12) && streamId == null)
                throw new Exception("Stream is required for class 11 and 12");

            // 1️⃣ Fetch students (STREAM FILTER APPLIED HERE ONLY)
            var studentQuery = _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.SectionId == sectionId &&
                    s.IsActive == true);

            if (classId == 11 || classId == 12)
                studentQuery = studentQuery.Where(s => s.StreamId == streamId);

            var students = await studentQuery
                .OrderBy(s => s.RollNo)
                .Select(s => new
                {
                    s.StudentId,
                    s.UserId,
                    s.RollNo,
                    s.AdmissionNo,
                    FullName = s.FirstName + " " + s.LastName
                })
                .ToListAsync();

            if (!students.Any())
            {
                return new AttendanceFetchResponse
                {
                    AttendanceDate = date,
                    SchoolId = schoolId,
                    ClassId = classId,
                    SectionId = sectionId,
                    Students = new List<AttendanceStudentDto>()
                };
            }

            var userIds = students.Select(s => s.UserId).ToList();

            // 2️⃣ Approved leave
            var leaveUserIds = await _context.Leaves
                .Where(l =>
                    l.SchoolId == schoolId &&
                    l.Status == "Approved" &&
                    date >= l.FromDate &&
                    date <= l.ToDate)
                .Select(l => l.UserId)
                .ToListAsync();

            // 3️⃣ Attendance (NO stream filtering here)
            var attendance = await _context.Attendances
                .Where(a =>
                    a.SchoolId == schoolId &&
                    a.AttendanceDate == date &&
                    userIds.Contains(a.UserId))
                .ToListAsync();

            var result = students.Select(s =>
            {
                if (leaveUserIds.Contains(s.UserId))
                {
                    return new AttendanceStudentDto
                    {
                        StudentId = s.StudentId,
                        UserId = (int)s.UserId,
                        RollNo = (int)s.RollNo,
                        AdmissionNo = s.AdmissionNo!,
                        FullName = s.FullName,
                        Status = "Leave",
                        IsEditable = false
                    };
                }

                var att = attendance.FirstOrDefault(a => a.UserId == s.UserId);

                return new AttendanceStudentDto
                {
                    StudentId = s.StudentId,
                    UserId = (int)s.UserId,
                    RollNo = (int)s.RollNo,
                    AdmissionNo = s.AdmissionNo!,
                    FullName = s.FullName,
                    Status = att?.Status ?? "Absent",
                    IsEditable = true
                };
            }).ToList();

            return new AttendanceFetchResponse
            {
                AttendanceDate = date,
                SchoolId = schoolId,
                ClassId = classId,
                SectionId = sectionId,
                Students = result
            };
        }



        public async Task SaveAttendanceAsync(AttendanceMarkRequest req)
        {
            // 🔐 Stream validation only for 11–12
            if ((req.ClassId == 11 || req.ClassId == 12) && req.StreamId == null)
                throw new Exception("Stream is required for class 11 and 12");

            // 1️⃣ Approved leave
            var leaveUserIds = await _context.Leaves
                .Where(l =>
                    l.SchoolId == req.SchoolId &&
                    l.Status == "Approved" &&
                    req.AttendanceDate >= l.FromDate &&
                    req.AttendanceDate <= l.ToDate)
                .Select(l => l.UserId)
                .ToListAsync();

            foreach (var record in req.Records)
            {
                if (leaveUserIds.Contains(record.UserId))
                    continue;

                var existing = await _context.Attendances.FirstOrDefaultAsync(a =>
                    a.SchoolId == req.SchoolId &&
                    a.UserId == record.UserId &&
                    a.AttendanceDate == req.AttendanceDate);

                if (existing != null)
                {
                    existing.Status = record.Status;
                    existing.Source = "Teacher";
                }
                else
                {
                    _context.Attendances.Add(new Attendance
                    {
                        SchoolId = req.SchoolId,
                        UserId = record.UserId,
                        AttendanceDate = req.AttendanceDate,
                        Status = record.Status,
                        Source = "Teacher"
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ApiResult<List<TeacherProfileDto>>> GetTeachersDirectoryAsync(int schoolId)
        {
            var teachers = await _context.Teachers
                .AsNoTracking()
                .Where(t => t.SchoolId == schoolId && t.IsActive == true)
                .Select(t => new TeacherProfileDto
                {
                    TeacherId = t.TeacherId,
                    FullName = t.FullName,
                    Phone = t.Phone,
                    Email = t.Email,
                    Qualification = t.Qualification
                })
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return ApiResult<List<TeacherProfileDto>>.Ok(teachers);
        }

        // Get specific profile details
        public async Task<ApiResult<TeacherProfileDto>> GetTeacherProfileAsync(int teacherId, int schoolId)
        {
            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId && t.SchoolId == schoolId);

            if (teacher == null) return ApiResult<TeacherProfileDto>.Fail("Teacher not found.");

            var dto = new TeacherProfileDto
            {
                TeacherId = teacher.TeacherId,
                SchoolId = teacher.SchoolId,
                FullName = teacher.FullName,
                Phone = teacher.Phone,
                Email = teacher.Email,
                JoiningDate = teacher.JoiningDate,
                Qualification = teacher.Qualification,
                IsActive = teacher.IsActive
            };

            return ApiResult<TeacherProfileDto>.Ok(dto);
        }

        // Save/Update teacher details
        public async Task<ApiResult<bool>> UpdateTeacherProfileAsync(TeacherProfileDto dto)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.TeacherId == dto.TeacherId && t.SchoolId == dto.SchoolId);

            if (teacher == null) return ApiResult<bool>.Fail("Teacher record not found.");

            teacher.FullName = dto.FullName;
            teacher.Phone = dto.Phone;
            teacher.Email = dto.Email;
            teacher.JoiningDate = dto.JoiningDate;
            teacher.Qualification = dto.Qualification;
            teacher.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "Teacher profile updated successfully.");
        }
    }
}

