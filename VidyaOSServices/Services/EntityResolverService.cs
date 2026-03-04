using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSServices.Services
{
    public class EntityResolverService
    {
        private readonly VidyaOsContext _context;

        public EntityResolverService(VidyaOsContext context)
        {
            _context = context;
        }

        // =============================
        // STUDENT RESOLVER
        // =============================

        public async Task<(int? StudentId, int? UserId)> ResolveStudentAsync(
            int schoolId,
            string identifier)
        {
            identifier = identifier.Trim().ToLower();

            // Try Admission Number first
            var student = await _context.Students
                .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                .Where(s =>
                    s.AdmissionNo!.ToLower() == identifier ||
                    (s.FirstName + " " + s.LastName).ToLower().Contains(identifier))
                .Select(s => new
                {
                    s.StudentId,
                    s.UserId
                })
                .FirstOrDefaultAsync();

            if (student == null)
                return (null, null);

            return (student.StudentId, student.UserId);
        }

        // =============================
        // TEACHER RESOLVER
        // =============================

        public async Task<(int? TeacherId, int? UserId)> ResolveTeacherAsync(
            int schoolId,
            string teacherName)
        {
            teacherName = teacherName.Trim().ToLower();

            var teacher = await _context.Teachers
                .Where(t => t.SchoolId == schoolId && t.IsActive == true)
                .Where(t => t.FullName!.ToLower().Contains(teacherName))
                .Select(t => new
                {
                    t.TeacherId,
                    t.UserId
                })
                .FirstOrDefaultAsync();

            if (teacher == null)
                return (null, null);

            return (teacher.TeacherId, teacher.UserId);
        }

        // =============================
        // EXAM RESOLVER
        // =============================

        public async Task<int?> ResolveExamIdAsync(int schoolId, string examName)
        {
            examName = examName.Trim().ToLower();

            var examId = await _context.Exams
                .Where(e => e.SchoolId == schoolId && e.IsActive == true)
                .Where(e => e.ExamName!.ToLower().Contains(examName))
                .Select(e => e.ExamId)
                .FirstOrDefaultAsync();

            return examId == 0 ? null : examId;
        }

        // =============================
        // SUBJECT RESOLVER
        // =============================

        public async Task<int?> ResolveSubjectIdAsync(
            int schoolId,
            string subjectName)
        {
            subjectName = subjectName.Trim().ToLower();

            var subjectId = await _context.Subjects
                .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                .Where(s => s.SubjectName!.ToLower().Contains(subjectName))
                .Select(s => s.SubjectId)
                .FirstOrDefaultAsync();

            return subjectId == 0 ? null : subjectId;
        }

        // =============================
        // CLASS RESOLVER
        // =============================

        public async Task<int?> ResolveClassIdAsync(
            int schoolId,
            string className)
        {
            className = className.Trim().ToLower();

            var classId = await _context.Classes
                .Where(c => c.SchoolId == schoolId && c.IsActive == true)
                .Where(c => c.ClassName!.ToLower().Contains(className))
                .Select(c => c.ClassId)
                .FirstOrDefaultAsync();

            return classId == 0 ? null : classId;
        }

        // =============================
        // SECTION RESOLVER
        // =============================

        public async Task<int?> ResolveSectionIdAsync(
            int schoolId,
            string sectionName)
        {
            sectionName = sectionName.Trim().ToLower();

            var sectionId = await _context.Sections
                .Where(s => s.SchoolId == schoolId)
                .Where(s => s.SectionName!.ToLower().Contains(sectionName))
                .Select(s => s.SectionId)
                .FirstOrDefaultAsync();

            return sectionId == 0 ? null : sectionId;
        }

        // =============================
        // STREAM RESOLVER
        // =============================

        public async Task<int?> ResolveStreamIdAsync(
            int schoolId,
            string streamName)
        {
            streamName = streamName.Trim().ToLower();

            var streamId = await _context.Streams
                .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                .Where(s => s.StreamName!.ToLower().Contains(streamName))
                .Select(s => s.StreamId)
                .FirstOrDefaultAsync();

            return streamId == 0 ? null : streamId;
        }

        // =============================
        // USER RESOLVER
        // =============================

        public async Task<int?> ResolveUserIdFromStudentAsync(
            int schoolId,
            string studentName)
        {
            studentName = studentName.Trim().ToLower();

            var userId = await _context.Students
                .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                .Where(s =>
                    (s.FirstName + " " + s.LastName)
                    .ToLower()
                    .Contains(studentName))
                .Select(s => s.UserId)
                .FirstOrDefaultAsync();

            return userId == 0 ? null : userId;
        }

        public async Task<int?> ResolveUserIdFromTeacherAsync(
            int schoolId,
            string teacherName)
        {
            teacherName = teacherName.Trim().ToLower();

            var userId = await _context.Teachers
                .Where(t => t.SchoolId == schoolId && t.IsActive == true)
                .Where(t => t.FullName!.ToLower().Contains(teacherName))
                .Select(t => t.UserId)
                .FirstOrDefaultAsync();

            return userId == 0 ? null : userId;
        }

        // =============================
        // GET STUDENT DETAILS (SAFE)
        // =============================

        public async Task<object?> GetStudentBasicInfoAsync(
            int schoolId,
            int studentId)
        {
            return await _context.Students
                .Where(s => s.SchoolId == schoolId && s.StudentId == studentId)
                .Select(s => new
                {
                    StudentName = s.FirstName + " " + s.LastName,
                    s.RollNo,
                    s.AdmissionNo,
                    s.ClassId,
                    s.SectionId
                })
                .FirstOrDefaultAsync();
        }

    } 
}
