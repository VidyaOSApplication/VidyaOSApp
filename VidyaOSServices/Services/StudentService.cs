using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using VidyaOSHelper.SchoolHelper;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;

namespace VidyaOSServices.Services
{
    public class StudentService
    {
        private readonly VidyaOsContext _context;
        private readonly StudentHelper _studentHelper;
        public StudentService(VidyaOsContext context,StudentHelper helper)
        {
            _context = context;
            _studentHelper = helper;
        }

        public async Task<ApiResult<List<Student>>> GetAllStudentsBySchoolAsync(int schoolId)
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.SchoolId == schoolId)
                    .AsNoTracking()
                    .ToListAsync();
                return ApiResult<List<Student>>.Ok(students);
            }
            catch (Exception ex)
            {
                return ApiResult<List<Student>>.Fail("Error fetching students: " + ex.Message);
            }
        }
        public async Task<ApiResult<StudentRegisterResponse>> RegisterStudentAsync(StudentRegisterRequest req)
        {
            if (req == null) return ApiResult<StudentRegisterResponse>.Fail("Request cannot be null.");
            if (req.SchoolId <= 0) return ApiResult<StudentRegisterResponse>.Fail("Invalid school.");

            // ---------- STREAM VALIDATION (New Logic) ----------
            if (req.ClassId == 11 || req.ClassId == 12)
            {
                if (req.StreamId == null)
                    return ApiResult<StudentRegisterResponse>.Fail("Stream is required for class 11 and 12.");

                // Ensure the stream exists for THIS school and THIS class
                var isValidStream = await _context.Streams.AnyAsync(st =>
                    st.StreamId == req.StreamId &&
                    st.SchoolId == req.SchoolId &&
                    st.ClassId == req.ClassId);

                if (!isValidStream)
                    return ApiResult<StudentRegisterResponse>.Fail("Selected stream is not valid for this class.");
            }
            else
            {
                req.StreamId = null; // Ensure stream is null for lower classes
            }

            // ---------- BASIC VALIDATION ----------
            if (string.IsNullOrWhiteSpace(req.FirstName))
                return ApiResult<StudentRegisterResponse>.Fail("First name is required.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dob = DateOnly.FromDateTime(req.DOB);

            if (dob > today) return ApiResult<StudentRegisterResponse>.Fail("Date of birth cannot be in the future.");

            int age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            if (age < 3) return ApiResult<StudentRegisterResponse>.Fail("Student must be at least 3 years old.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(req.ParentPhone.Trim(), @"^[6-9]\d{9}$"))
                return ApiResult<StudentRegisterResponse>.Fail("Invalid 10-digit parent phone number.");

            // ---------- DUPLICATE CHECK ----------
            bool exists = await _context.Students.AnyAsync(s =>
                s.SchoolId == req.SchoolId &&
                s.AcademicYear == req.AcademicYear &&
                s.FirstName!.ToLower() == req.FirstName.ToLower() &&
                s.Dob == dob &&
                s.ParentPhone == req.ParentPhone
            );

            if (exists) return ApiResult<StudentRegisterResponse>.Fail("Student already exists with same name, DOB, and parent phone.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var school = await _context.Schools.FirstOrDefaultAsync(s => s.SchoolId == req.SchoolId);
                if (school == null) return ApiResult<StudentRegisterResponse>.Fail("School not found.");

                // Helper Generations
                string admissionNo = await _studentHelper.GenerateAdmissionNoAsync(req.SchoolId, req.AdmissionDate.Year, school.SchoolCode!);
                int rollNo = await _studentHelper.GenerateRollNoAsync(req.SectionId);
                string username = await _studentHelper.GenerateStudentUsernameAsync(req.FirstName, req.LastName);
                string tempPassword = $"{req.FirstName}{req.DOB.Year}";
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                // Create User record
                var user = new User
                {
                    SchoolId = req.SchoolId,
                    Username = username,
                    PasswordHash = passwordHash,
                    Role = "Student",
                    Email = req.Email,
                    Phone = req.ParentPhone,
                    IsFirstLogin = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Student record
                var student = new Student
                {
                    SchoolId = req.SchoolId,
                    UserId = user.UserId,
                    AdmissionNo = admissionNo,
                    RollNo = rollNo,
                    FirstName = req.FirstName,
                    LastName = req.LastName,
                    Gender = req.Gender,
                    Dob = dob,
                    ClassId = req.ClassId,
                    SectionId = req.SectionId,
                    StreamId = req.StreamId,
                    AcademicYear = req.AcademicYear,
                    AdmissionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    FatherName = req.FatherName,
                    MotherName = req.MotherName,
                    ParentPhone = req.ParentPhone,
                    AddressLine1 = req.AddressLine1,
                    City = req.City,
                    State = req.State,
                    StudentStatus = "Active",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return ApiResult<StudentRegisterResponse>.Ok(new StudentRegisterResponse
                {
                    StudentId = student.StudentId,
                    AdmissionNo = admissionNo,
                    Username = username,
                    TempPassword = tempPassword
                }, "Student registered successfully.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ApiResult<StudentRegisterResponse>.Fail("Database error: " + ex.Message);
            }
        }
        public async Task<ApiResult<List<BulkMarksEntryDto>>> GetMarksEntryListAsync(
            int schoolId, int examId, int classId, int sectionId, int subjectId, int? streamId)
        {
            var query = _context.Students.AsNoTracking()
                .Where(s => s.SchoolId == schoolId && s.ClassId == classId && s.SectionId == sectionId && s.IsActive == true);

            if ((classId == 11 || classId == 12) && streamId.HasValue && streamId > 0)
            {
                query = query.Where(s => s.StreamId == streamId);
            }

            var students = await query
                .OrderBy(s => s.RollNo)
                .Select(s => new BulkMarksEntryDto
                {
                    StudentId = s.StudentId,
                    RollNo = s.RollNo,
                    FullName = s.FirstName + " " + s.LastName,
                    AdmissionNo = s.AdmissionNo,
                    MarksObtained = _context.StudentMarks
                        .Where(m => m.ExamId == examId && m.SubjectId == subjectId && m.StudentId == s.StudentId)
                        .Select(m => (int?)m.MarksObtained).FirstOrDefault(),
                    // Source MaxMarks from ExamSubjects configuration
                    MaxMarks = _context.ExamSubjects
                        .Where(es => es.ExamId == examId && es.SubjectId == subjectId && es.ClassId == classId)
                        .Select(es => es.MaxMarks).FirstOrDefault()
                }).ToListAsync();

            students.ForEach(s => { if (s.MaxMarks == 0) s.MaxMarks = 100; });

            return ApiResult<List<BulkMarksEntryDto>>.Ok(students);
        }

        public async Task<ApiResult<ExamSelectionDataDto>> GetExamSelectionDataAsync(int schoolId)
        {
            var data = new ExamSelectionDataDto
            {
                Exams = await _context.Exams
                    .Where(e => e.SchoolId == schoolId && e.IsActive == true)
                    .Select(e => new LookUpDto { Id = e.ExamId, Name = e.ExamName }).ToListAsync(),

                Classes = await _context.Classes
                    .Where(c => c.SchoolId == schoolId && c.IsActive == true)
                    .Select(c => new LookUpDto { Id = c.ClassId, Name = c.ClassName }).ToListAsync(),

                Subjects = await _context.Subjects
                    .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                    .Select(s => new LookUpDto { Id = s.SubjectId, Name = s.SubjectName }).ToListAsync()
            };

            return ApiResult<ExamSelectionDataDto>.Ok(data);
        }

    }
}
