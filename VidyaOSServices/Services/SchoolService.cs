using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public async Task<ApiResult<StudentRegisterResponse>> RegisterStudentAsync(
        StudentRegisterRequest req)
        {
            // ---------- Validation ----------
            if (req == null)
                return ApiResult<StudentRegisterResponse>.Fail("Request cannot be null.");

            if (req.SchoolId <= 0)
                return ApiResult<StudentRegisterResponse>.Fail("Invalid school.");

            if (string.IsNullOrWhiteSpace(req.FirstName))
                return ApiResult<StudentRegisterResponse>.Fail("First name is required.");

            if (req.DOB == default)
                return ApiResult<StudentRegisterResponse>.Fail("Date of birth is required.");

            if (req.DOB == default)
                return ApiResult<StudentRegisterResponse>
                    .Fail("Date of birth is required.");

            // ✅ ADD THIS BLOCK
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dob = DateOnly.FromDateTime(req.DOB);

            // ❌ Future DOB
            if (dob > today)
                return ApiResult<StudentRegisterResponse>
                    .Fail("Date of birth cannot be in the future.");

            // ❌ Age calculation
            int age = today.Year - dob.Year;
            if (dob > today.AddYears(-age))
                age--;

            // ❌ Age limits (business rule)
            if (age < 3 )
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
                // ---------- Admission No ----------
                var school = await _context.Schools
                                                  .FirstOrDefaultAsync(s => s.SchoolId == req.SchoolId);

                if (school == null)
                    return ApiResult<StudentRegisterResponse>
                        .Fail("School not found.");

                int admissionYear = int.Parse(req.AcademicYear);
                string admissionNo = await _schoolHelper.GenerateAdmissionNoAsync(
                    req.SchoolId,
                    admissionYear,
                    school.SchoolCode!
                );

                // ---------- Roll No ----------
                int rollNo = await _schoolHelper.GenerateRollNoAsync(req.SectionId);

                // ---------- User ----------
                string tempPassword = $"{req.FirstName}@{req.DOB.Year}";
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                var user = new User
                {
                    SchoolId = req.SchoolId,
                    Username = admissionNo,
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

                // ---------- Student ----------
                var student = new Student
                {
                    SchoolId = req.SchoolId,
                    UserId = user.UserId,
                    AdmissionNo = admissionNo,
                    RollNo = rollNo,
                    FirstName = req.FirstName,
                    LastName = req.LastName,
                    Gender = req.Gender,
                    Dob = DateOnly.FromDateTime(req.DOB),
                    ClassId = req.ClassId,
                    SectionId = req.SectionId,
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
                        Username = admissionNo,
                        TempPassword = tempPassword
                    },
                    "Student registered successfully."
                );
            }
            catch
            {
                await tx.RollbackAsync();
                throw; // system failure handled globally
            }
        }




        public async Task RegisterSchoolAsync(RegisterSchoolRequest req)
        {
            // Basic API validation
            if (string.IsNullOrWhiteSpace(req.SchoolName))
                throw new Exception("School name is required");

            if (string.IsNullOrWhiteSpace(req.AdminUsername))
                throw new Exception("Admin username is required");

            if (string.IsNullOrWhiteSpace(req.AdminPassword))
                throw new Exception("Admin password is required");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(req.AdminPassword);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_RegisterSchool " +
                "@SchoolName,@SchoolCode,@RegistrationNumber,@YearOfFoundation," +
                "@BoardType,@AffiliationNumber,@Email,@Phone,@AddressLine1," +
                "@City,@State,@Pincode,@AdminUsername,@AdminPasswordHash",

                new SqlParameter("@SchoolName", req.SchoolName),
                new SqlParameter("@SchoolCode", req.SchoolCode ?? ""),
                new SqlParameter("@RegistrationNumber", req.RegistrationNumber ?? ""),
                new SqlParameter("@YearOfFoundation", req.YearOfFoundation ?? 0),
                new SqlParameter("@BoardType", req.BoardType ?? ""),
                new SqlParameter("@AffiliationNumber", req.AffiliationNumber ?? ""),
                new SqlParameter("@Email", req.Email ?? ""),
                new SqlParameter("@Phone", req.Phone ?? ""),
                new SqlParameter("@AddressLine1", req.AddressLine1 ?? ""),
                new SqlParameter("@City", req.City ?? ""),
                new SqlParameter("@State", req.State ?? ""),
                new SqlParameter("@Pincode", req.Pincode ?? ""),
                new SqlParameter("@AdminUsername", req.AdminUsername),
                new SqlParameter("@AdminPasswordHash", passwordHash)
            );
        }



    }
}
