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
        public async Task<ApiResult<StudentRegisterResponse>> RegisterStudentAsync(StudentRegisterRequest req)
        {
            // 1. ---------- Basic Null & Identity Validation ----------
            if (req == null)
                return ApiResult<StudentRegisterResponse>.Fail("Request cannot be null.");

            if (req.SchoolId <= 0)
                return ApiResult<StudentRegisterResponse>.Fail("Invalid school.");

            // 2. ---------- Stream Validation (Class 11 & 12) ----------
            if ((req.ClassId == 11 || req.ClassId == 12) && req.StreamId == null)
            {
                return ApiResult<StudentRegisterResponse>.Fail("Stream is required for class 11 and 12.");
            }

            // 3. ---------- Required Fields Validation ----------
            if (string.IsNullOrWhiteSpace(req.FirstName))
                return ApiResult<StudentRegisterResponse>.Fail("First name is required.");

            if (string.IsNullOrWhiteSpace(req.Category))
                return ApiResult<StudentRegisterResponse>.Fail("Student category is required.");

            if (req.DOB == default)
                return ApiResult<StudentRegisterResponse>.Fail("Date of birth is required.");

            // 4. ---------- DOB & Age Validation ----------
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dob = DateOnly.FromDateTime(req.DOB);

            if (dob > today)
                return ApiResult<StudentRegisterResponse>.Fail("Date of birth cannot be in the future.");

            int age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;

            if (age < 3)
                return ApiResult<StudentRegisterResponse>.Fail("Student should not be less than 3 years old.");

            // 5. ---------- Infrastructure & Contact Validation ----------
            if (req.ClassId <= 0 || req.SectionId <= 0)
                return ApiResult<StudentRegisterResponse>.Fail("Class and section are required.");

            if (string.IsNullOrWhiteSpace(req.AcademicYear))
                return ApiResult<StudentRegisterResponse>.Fail("Academic year is required.");

            if (string.IsNullOrWhiteSpace(req.ParentPhone))
                return ApiResult<StudentRegisterResponse>.Fail("Parent phone is required.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(req.ParentPhone.Trim(), @"^[6-9]\d{9}$"))
            {
                return ApiResult<StudentRegisterResponse>.Fail("Invalid parent phone number.");
            }

            // 6. ---------- Duplicate Check ----------
            bool exists = await _context.Students.AsNoTracking().AnyAsync(s =>
                s.SchoolId == req.SchoolId &&
                s.AcademicYear == req.AcademicYear &&
                s.FirstName!.ToLower() == req.FirstName.ToLower() &&
                s.Dob == dob &&
                s.ParentPhone == req.ParentPhone
            );

            if (exists)
            {
                return ApiResult<StudentRegisterResponse>.Fail(
                    "Student already exists with same name, date of birth and parent mobile number."
                );
            }

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 7. ---------- School Context ----------
                var school = await _context.Schools.FirstOrDefaultAsync(s => s.SchoolId == req.SchoolId);
                if (school == null)
                    return ApiResult<StudentRegisterResponse>.Fail("School not found.");

                // 8. ---------- Helper Generations (Admission, Roll, Username) ----------
                int admissionYear = req.AdmissionDate.Year;
                string admissionNo = await _studentHelper.GenerateAdmissionNoAsync(req.SchoolId, admissionYear, school.SchoolCode!);
                int rollNo = await _studentHelper.GenerateRollNoAsync(req.SectionId);
                string username = await _studentHelper.GenerateStudentUsernameAsync(req.FirstName, req.LastName);

                // 9. ---------- Identity Security ----------
                string tempPassword = $"{req.FirstName}{req.DOB.Year}";
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                // 10. ---------- Create User Entity ----------
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

                // 11. ---------- Create Student Entity ----------
                var student = new Student
                {
                    SchoolId = req.SchoolId,
                    UserId = user.UserId,
                    AdmissionNo = admissionNo,
                    RollNo = rollNo,
                    FirstName = req.FirstName.Trim(),
                    LastName = req.LastName?.Trim(),
                    Gender = req.Gender,
                    Category = req.Category, // 🚀 NEW FIELD MAPPED HERE
                    Dob = dob,
                    ClassId = req.ClassId,
                    SectionId = req.SectionId,
                    StreamId = (req.ClassId == 11 || req.ClassId == 12) ? req.StreamId : null,
                    AcademicYear = req.AcademicYear,
                    AdmissionDate = DateOnly.FromDateTime(req.AdmissionDate),
                    FatherName = req.FatherName?.Trim(),
                    MotherName = req.MotherName?.Trim(),
                    ParentPhone = req.ParentPhone.Trim(),
                    AddressLine1 = req.AddressLine1?.Trim(),
                    City = req.City?.Trim(),
                    State = req.State?.Trim(),
                    StudentStatus = "Active",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return ApiResult<StudentRegisterResponse>.Ok(
                    new StudentRegisterResponse
                    {
                        StudentId = student.StudentId,
                        AdmissionNo = admissionNo,
                        Username = username,
                        TempPassword = tempPassword
                    },
                    "Student registered successfully."
                );
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                // Log the exception here if you have a logger
                return ApiResult<StudentRegisterResponse>.Fail($"Registration failed: {ex.Message}");
            }
        }
        public async Task<ApiResult<List<BulkMarksEntryDto>>> GetMarksEntryListAsync(int examId, int classId, int subjectId, int? sectionId)
        {
            var students = await _context.Students
                .Where(s => s.ClassId == classId && (!sectionId.HasValue || s.SectionId == sectionId))
                .OrderBy(s => s.RollNo) // Important: Sort by Roll No
                .Select(s => new BulkMarksEntryDto
                {
                    StudentId = s.StudentId,
                    RollNo = s.RollNo,
                    FullName = s.FirstName + " " + s.LastName,
                    AdmissionNo = s.AdmissionNo,
                    MarksObtained = _context.StudentMarks
                        .Where(m => m.ExamId == examId && m.SubjectId == subjectId && m.StudentId == s.StudentId)
                        .Select(m => m.MarksObtained).FirstOrDefault(),
                    MaxMarks = _context.StudentMarks
                        .Where(m => m.ExamId == examId && m.SubjectId == subjectId && m.StudentId == s.StudentId)
                        .Select(m => m.MaxMarks).FirstOrDefault() == 0 ? 100 : _context.StudentMarks
                        .Where(m => m.ExamId == examId && m.SubjectId == subjectId && m.StudentId == s.StudentId)
                        .Select(m => m.MaxMarks).FirstOrDefault()
                }).ToListAsync();

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
