using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSHelper
{
    public class TeacherHelper
    {

        private readonly VidyaOsContext _context;
        public TeacherHelper(VidyaOsContext context)
        {
            _context = context;
        }

        // ---------- USERNAME GENERATOR ----------
        public async Task<string> GenerateTeacherUsernameAsync(string fullName)
        {
            var parts = fullName
                .Trim()
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var baseUsername = parts.Length > 1
                ? $"{parts[0]}.{parts[^1]}"
                : parts[0];

            var username = baseUsername.ToLower();
            int counter = 1;

            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username.ToLower();
        }

        // ---------- PASSWORD GENERATOR ----------
        public string GenerateTempPassword(string fullName)
        {
            var firstName = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

            var year = DateTime.UtcNow.Year;

            return $"{firstName}{year}";
        }
    }
}

