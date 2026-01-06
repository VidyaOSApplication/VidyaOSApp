using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;
using VidyaOSHelper.PasswordHelp;

namespace VidyaOSServices.Services
{
    public class SchoolService
    {
        private readonly VidyaOsContext _context;
        public SchoolService(VidyaOsContext context)
        {
            _context = context;
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

            string passwordHash = PasswordHelper.Hash(req.AdminPassword);

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
