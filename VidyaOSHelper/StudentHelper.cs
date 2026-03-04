using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSHelper
{
    public class StudentHelper
    {
        private readonly VidyaOsContext _context;
        public StudentHelper(VidyaOsContext context)
        {
            _context = context;
        }
        public async Task<string> GenerateStudentUsernameAsync(
    string firstName, string? lastName)
        {
            var baseUsername = string.IsNullOrWhiteSpace(lastName)
                ? firstName.ToLower()
                : $"{firstName.ToLower()}.{lastName.ToLower()}";

            var username = baseUsername;
            int counter = 1;

            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username.ToLower();
        }
        public async Task<string> GenerateAdmissionNoAsync(
        int schoolId, int year, string schoolCode)
        {
            var seq = await _context.AdmissionYearSequences
                .FirstOrDefaultAsync(x =>
                    x.SchoolId == schoolId &&
                    x.AdmissionYear == year);

            if (seq == null)
            {
                seq = new AdmissionYearSequence
                {
                    SchoolId = schoolId,
                    AdmissionYear = year,
                    LastSeq = 1
                };
                _context.AdmissionYearSequences.Add(seq);
            }
            else
            {
                seq.LastSeq += 1;
            }

            await _context.SaveChangesAsync();

            return $"{schoolCode}-{year}-{seq.LastSeq:D4}";
        }

        public async Task<int> GenerateRollNoAsync(int sectionId)
        {
            var section = await _context.Sections
                .FirstOrDefaultAsync(s => s.SectionId == sectionId);

            if (section == null)
                throw new InvalidOperationException("Section not found.");

            // ✅ safe increment (handles NULL)
            int next = (section.RollSeq ?? 0) + 1;

            section.RollSeq = next;

            await _context.SaveChangesAsync();

            return next;
        }
    }
}
