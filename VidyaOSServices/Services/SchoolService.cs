using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;

namespace VidyaOSServices.Services
{
    public class SchoolService
    {
        private readonly VidyaOsContext _context;
        public SchoolService(VidyaOsContext context)
        {
            _context = context;
        }
        public async Task<StudentRegisterResponse> RegisterStudentAsync(StudentRegisterRequest req)
        {
            try
            {
                string tempPassword = $"{req.FirstName}@{req.DOB.Year}";
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                var result = _context.StudentRegisterResponses
                    .FromSqlRaw(
                        @"EXEC sp_RegisterStudent
                  @SchoolId,@FirstName,@LastName,@Gender,@DOB,
                  @ClassId,@SectionId,@AcademicYear,
                  @FatherName,@ParentPhone,@Email,@PasswordHash,
                  @AddressLine1,@City,@State",
                        new SqlParameter("@SchoolId", req.SchoolId),
                        new SqlParameter("@FirstName", req.FirstName),
                        new SqlParameter("@LastName", req.LastName),
                        new SqlParameter("@Gender", req.Gender),
                        new SqlParameter("@DOB", req.DOB),
                        new SqlParameter("@ClassId", req.ClassId),
                        new SqlParameter("@SectionId", req.SectionId),
                        new SqlParameter("@AcademicYear", req.AcademicYear),
                        new SqlParameter("@FatherName", req.FatherName),
                        new SqlParameter("@ParentPhone", req.ParentPhone),
                        new SqlParameter("@Email", req.Email ?? ""),
                        new SqlParameter("@PasswordHash", passwordHash),
                        new SqlParameter("@AddressLine1", req.AddressLine1),
                        new SqlParameter("@City", req.City),
                        new SqlParameter("@State", req.State)
                    )
                    .AsNoTracking()
                    .AsEnumerable()
                    .FirstOrDefault();

                if (result == null)
                    throw new Exception("Student registration failed.");

                result.TempPassword = tempPassword;
                return result;
            }
            catch (SqlException ex)
            {
                // 🎯 HANDLE BUSINESS ERRORS FROM SQL
                if (ex.Message.Contains("Student already exists"))
                {
                    throw new InvalidOperationException(
                        "Student already exists with same name, date of birth and parent mobile number."
                    );
                }

                throw; // unknown DB error
            }
        }


    }
}
