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
        public async Task<ApiResult<StudentRegisterResponse>> RegisterStudentAsync(
    StudentRegisterRequest req)
        {
            // ---------- Validation ----------
            if ((req.ClassId == 11 || req.ClassId == 12) && req.StreamId == null)
            {
                return ApiResult<StudentRegisterResponse>.Fail("Stream is required for class 11 and 12");
            }
            if (req == null)
                return ApiResult<StudentRegisterResponse>.Fail("Request cannot be null.");

            if (req.SchoolId <= 0)
                return ApiResult<StudentRegisterResponse>.Fail("Invalid school.");

            if (string.IsNullOrWhiteSpace(req.FirstName))
                return ApiResult<StudentRegisterResponse>.Fail("First name is required.");

            if (req.DOB == default)
                return ApiResult<StudentRegisterResponse>.Fail("Date of birth is required.");

            // ---------- DOB & AGE VALIDATION ----------
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dob = DateOnly.FromDateTime(req.DOB);

            if (dob > today)
                return ApiResult<StudentRegisterResponse>
                    .Fail("Date of birth cannot be in the future.");

            int age = today.Year - dob.Year;
            if (dob > today.AddYears(-age))
                age--;

            if (age < 3)
                return ApiResult<StudentRegisterResponse>
                    .Fail("Student should not be less than 3 years old.");

            if (req.ClassId <= 0 || req.SectionId <= 0)
                return ApiResult<StudentRegisterResponse>.Fail("Class and section are required.");

            if (string.IsNullOrWhiteSpace(req.AcademicYear))
                return ApiResult<StudentRegisterResponse>.Fail("Academic year is required.");

            if (string.IsNullOrWhiteSpace(req.ParentPhone))
                return ApiResult<StudentRegisterResponse>.Fail("Parent phone is required.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                req.ParentPhone.Trim(), @"^[6-9]\d{9}$"))
            {
                return ApiResult<StudentRegisterResponse>
                    .Fail("Invalid parent phone number.");
            }

            // ---------- Duplicate Check ----------
            bool exists = await _context.Students.AnyAsync(s =>
                s.SchoolId == req.SchoolId &&
                s.AcademicYear == req.AcademicYear &&
                s.FirstName!.ToLower() == req.FirstName.ToLower() &&
                s.Dob == DateOnly.FromDateTime(req.DOB) &&
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
                // ---------- School ----------
                var school = await _context.Schools
                    .FirstOrDefaultAsync(s => s.SchoolId == req.SchoolId);

                if (school == null)
                    return ApiResult<StudentRegisterResponse>.Fail("School not found.");

                // ---------- Admission No ----------
                int admissionYear = req.AdmissionDate.Year;

                string admissionNo = await _studentHelper.GenerateAdmissionNoAsync(
                    req.SchoolId,
                    admissionYear,
                    school.SchoolCode!
                );

                // ---------- Roll No ----------
                int rollNo = await _studentHelper.GenerateRollNoAsync(req.SectionId);

                // ---------- USERNAME (AUTO FROM NAME) ----------
                string username = await _studentHelper.GenerateStudentUsernameAsync(
                    req.FirstName, req.LastName);

                // ---------- PASSWORD (FirstName + BirthYear) ----------
                string tempPassword = $"{req.FirstName}{req.DOB.Year}";
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                // ---------- User ----------
                var user = new User
                {
                    SchoolId = req.SchoolId,
                    Username = username,
                    PasswordHash = passwordHash,
                    Role = "Student",
                    Email = req.Email,
                    Phone = req.ParentPhone,
                    IsFirstLogin = true,     // 🔥 force change
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // ---------- Student ----------
                var student = new Student
                {
                    SchoolId = req.SchoolId,
                    UserId = user.UserId,           // 🔥 RELATION
                    AdmissionNo = admissionNo,
                    RollNo = rollNo,
                    FirstName = req.FirstName,
                    LastName = req.LastName,
                    Gender = req.Gender,
                    Dob = DateOnly.FromDateTime(req.DOB),
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
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        
    }
}
