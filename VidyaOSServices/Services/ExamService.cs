using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;

namespace VidyaOSServices.Services
{
    public class ExamService
    {
        private readonly VidyaOsContext _context;
        public ExamService(VidyaOsContext context)
        {
            _context = context;
        }
        public async Task<ApiResult<int>> CreateExamAsync(CreateExamRequest req)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var exam = new Exam
                {
                    SchoolId = req.SchoolId,
                    ExamName = req.ExamName,
                    AcademicYear = req.AcademicYear,
                    IsActive = true
                };

                _context.Exams.Add(exam);
                await _context.SaveChangesAsync();

                foreach (var classId in req.ClassIds)
                {
                    _context.ExamClasses.Add(new ExamClass
                    {
                        ExamId = exam.ExamId,
                        ClassId = classId
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return ApiResult<int>.Ok(exam.ExamId, "Exam created successfully.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ---------------- ADD SUBJECTS ----------------
        public async Task<ApiResult<string>> AddExamSubjectsAsync(AddExamSubjectsRequest req)
        {
            bool addedAny = false;

            foreach (var s in req.Subjects)
            {
                bool exists = await _context.ExamSubjects.AnyAsync(x =>
                    x.ExamId == req.ExamId &&
                    x.ClassId == req.ClassId &&
                    x.SubjectId == s.SubjectId);

                if (exists) continue;

                _context.ExamSubjects.Add(new ExamSubject
                {
                    ExamId = req.ExamId,
                    ClassId = req.ClassId,
                    SubjectId = s.SubjectId,
                    ExamDate = DateOnly.FromDateTime(s.ExamDate),
                    MaxMarks = s.MaxMarks
                });

                addedAny = true;
            }

            await _context.SaveChangesAsync();

            // ✅ UPDATE EXAM STATUS
            if (addedAny)
            {
                var exam = await _context.Exams.FindAsync(req.ExamId);
                if (exam != null && exam.Status == "Draft")
                {
                    exam.Status = "Subjects Assigned";
                    await _context.SaveChangesAsync();
                }
            }

            return ApiResult<string>.Ok("OK", "Subjects added.");
        }


        // ---------------- GET EXAM DETAILS ----------------
        public async Task<object> GetExamDetailsAsync(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);

            var subjects = await (
                from es in _context.ExamSubjects
                join s in _context.Subjects on es.SubjectId equals s.SubjectId
                select new
                {
                    es.ClassId,
                    s.SubjectName,
                    es.ExamDate,
                    es.MaxMarks
                }
            ).ToListAsync();

            return new
            {
                exam!.ExamName,
                exam.AcademicYear,
                Subjects = subjects
            };
        }

        // ---------------- SAVE MARKS ----------------
        public async Task<ApiResult<string>> SaveStudentMarksAsync(SaveStudentMarksRequest req)
        {
            var examSubject = await _context.ExamSubjects.FirstOrDefaultAsync(x =>
                x.ExamId == req.ExamId &&
                x.ClassId == req.ClassId &&
                x.SubjectId == req.SubjectId);

            if (examSubject == null)
                return ApiResult<string>.Fail("Invalid exam subject.");

            foreach (var m in req.Marks)
            {
                var existing = await _context.StudentMarks.FirstOrDefaultAsync(x =>
                    x.StudentId == m.StudentId &&
                    x.ExamId == req.ExamId &&
                    x.ClassId == req.ClassId &&
                    x.SubjectId == req.SubjectId);

                if (existing != null)
                {
                    existing.MarksObtained = m.MarksObtained;
                    existing.IsAbsent = m.IsAbsent;
                }
                else
                {
                    _context.StudentMarks.Add(new StudentMark
                    {
                        SchoolId = req.SchoolId,
                        StudentId = m.StudentId,
                        ExamId = req.ExamId,
                        ClassId = req.ClassId,
                        SubjectId = req.SubjectId,   // ✅ FIXED
                        MarksObtained = m.MarksObtained,
                        MaxMarks = examSubject.MaxMarks,
                        IsAbsent = m.IsAbsent
                    });
                }
            }

            await _context.SaveChangesAsync();

            // ✅ CHECK IF ALL SUBJECTS HAVE MARKS
            var totalSubjects = await _context.ExamSubjects
                .Where(x => x.ExamId == req.ExamId)
                .Select(x => new { x.ClassId, x.SubjectId })
                .Distinct()
                .CountAsync();

            var subjectsWithMarks = await _context.StudentMarks
                .Where(x => x.ExamId == req.ExamId)
                .Select(x => new { x.ClassId, x.SubjectId })
                .Distinct()
                .CountAsync();

            if (totalSubjects == subjectsWithMarks && totalSubjects > 0)
            {
                var exam = await _context.Exams.FindAsync(req.ExamId);
                if (exam != null)
                    exam.Status = "Marks Entered";

                await _context.SaveChangesAsync();
            }

            return ApiResult<string>.Ok("OK", "Marks saved successfully.");
        }




        // ---------------- STUDENT RESULT ----------------
        public async Task<object> GetStudentResultAsync(int studentId, int examId)
        {
            var marks = await _context.StudentMarks
                .Where(x => x.StudentId == studentId && x.ExamId == examId)
                .Select(x => new
                {
                    x.SubjectId,
                    x.MarksObtained,
                    x.MaxMarks,
                    x.IsAbsent
                })
                .ToListAsync();

            int total = marks.Sum(x => x.MaxMarks);
            int obtained = marks.Where(x => (bool)!x.IsAbsent).Sum(x => x.MarksObtained);

            return new
            {
                TotalMarks = total,
                ObtainedMarks = obtained,
                Percentage = total == 0 ? 0 : (obtained * 100.0 / total),
                Subjects = marks
            };
        }
        public async Task<ApiResult<List<ExamListResponse>>> GetExamsAsync(int schoolId)
        {
            if (schoolId <= 0)
                return ApiResult<List<ExamListResponse>>.Fail("Invalid school");

            var exams = await _context.Exams
                .Where(e => e.SchoolId == schoolId && e.IsActive == true)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ExamListResponse
                {
                    ExamId = e.ExamId,
                    ExamName = e.ExamName,
                    AcademicYear = e.AcademicYear,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Status = e.Status ?? "Draft",

                    Classes = e.ExamClasses
                        .Select(ec => new ExamClassDto
                        {
                            ClassId = ec.ClassId,
                           
                        })
                        .ToList()
                })
                .ToListAsync();

            return ApiResult<List<ExamListResponse>>.Ok(exams);
        }
        // ---------------- GET SUBJECTS FOR EXAM + CLASS ----------------
        public async Task<ApiResult<object>> GetExamToAddSubjectsAsync(
            int examId,
            int classId,
            int schoolId)
        {
            // 1. Validate exam
            var exam = await _context.Exams
                .FirstOrDefaultAsync(e =>
                    e.ExamId == examId &&
                    e.SchoolId == schoolId &&
                    e.IsActive == true);

            if (exam == null)
                return ApiResult<object>.Fail("Exam not found");

            // 2. Validate exam-class mapping
            bool validClass = await _context.ExamClasses.AnyAsync(ec =>
                ec.ExamId == examId &&
                ec.ClassId == classId);

            if (!validClass)
                return ApiResult<object>.Fail("Class not linked to exam");

            // 3. Load subjects for class
            var subjects = await _context.Subjects
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.IsActive == true)
                .Select(s => new
                {
                    s.SubjectId,
                    s.SubjectName
                })
                .ToListAsync();

            // 4. Already assigned subjects (edit mode ready)
            var assigned = await _context.ExamSubjects
                .Where(es =>
                    es.ExamId == examId &&
                    es.ClassId == classId)
                .Select(es => new
                {
                    es.SubjectId,
                    es.ExamDate,
                    es.MaxMarks
                })
                .ToListAsync();

            return ApiResult<object>.Ok(new
            {
                exam.ExamName,
                exam.AcademicYear,
                subjects,
                assignedSubjects = assigned
            });
        }
        public async Task<ApiResult<object>> GetAssignedSubjectsAsync(
    int examId,
    int classId,
    int schoolId)
        {
            var subjects = await (
                from es in _context.ExamSubjects
                join s in _context.Subjects on es.SubjectId equals s.SubjectId
                where es.ExamId == examId
                      && es.ClassId == classId
                      && s.SchoolId == schoolId   // ✅ CORRECT PLACE
                      && s.IsActive == true
                select new
                {
                    subjectId = s.SubjectId,
                    subjectName = s.SubjectName,
                    examDate = es.ExamDate,
                    maxMarks = es.MaxMarks
                }
            ).ToListAsync();

            return ApiResult<object>.Ok(new
            {
                examId,
                classId,
                subjects
            });
        }


        public async Task<object> GetStudentsForMarksAsync(
    int examId,
    int classId,
    int subjectId)
        {
            var students = await _context.Students
                .Where(s => s.ClassId == classId && s.IsActive == true)
                .OrderBy(s => s.RollNo)
                .Select(s => new
                {
                    s.StudentId,
                    s.RollNo,
                    Name = s.FirstName + " " + s.LastName,

                    Mark = _context.StudentMarks
                        .Where(m =>
                            m.StudentId == s.StudentId &&
                            m.ExamId == examId &&
                            m.SubjectId == subjectId)
                        .Select(m => new
                        {
                            m.MarksObtained,
                            m.IsAbsent
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return students;
        }


    }
}

