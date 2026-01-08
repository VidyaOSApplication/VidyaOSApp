using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSHelper.SchoolHelper
{
    public class SchoolHelper
    {
        private readonly VidyaOsContext _context;
        public SchoolHelper(VidyaOsContext context)
        {
            _context = context;
        }
        public class ApiResult<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public T? Data { get; set; }

            public static ApiResult<T> Ok(T data, string message = "")
                => new() { Success = true, Data = data, Message = message };

            public static ApiResult<T> Fail(string message)
                => new() { Success = false, Message = message };
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

            section.RollSeq += 1;
            await _context.SaveChangesAsync();

            return (int)section.RollSeq;
        }



    }
}
