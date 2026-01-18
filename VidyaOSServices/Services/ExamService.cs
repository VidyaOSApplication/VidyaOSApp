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
        public async Task<ApiResult<object>> GetScheduleSubjectsAsync(
    int examId,
    int classId,
    int schoolId)
        {
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.ExamId == examId &&
                    x.SchoolId == schoolId &&
                    x.IsActive == true);

            if (exam == null)
                return ApiResult<object>.Fail("Exam not found");

            // All syllabus subjects for class
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

            // Already scheduled subjects
            var scheduled = await _context.ExamSubjects
                .Where(es =>
                    es.ExamId == examId &&
                    es.ClassId == classId)
                .ToListAsync();

            var result = subjects.Select(s =>
            {
                var sch = scheduled.FirstOrDefault(x => x.SubjectId == s.SubjectId);
                return new
                {
                    s.SubjectId,
                    s.SubjectName,
                    ExamDate = sch?.ExamDate,
                    MaxMarks = sch?.MaxMarks
                };
            });

            return ApiResult<object>.Ok(new
            {
                exam.ExamName,
                classId,
                subjects = result
            });
        }


        public async Task<ApiResult<string>> SaveExamScheduleAsync(
    int examId,
    int classId,
    List<ScheduleSubjectDto> subjects)
        {
            foreach (var s in subjects)
            {
                var existing = await _context.ExamSubjects.FirstOrDefaultAsync(x =>
                    x.ExamId == examId &&
                    x.ClassId == classId &&
                    x.SubjectId == s.SubjectId);

                if (existing == null)
                {
                    _context.ExamSubjects.Add(new ExamSubject
                    {
                        ExamId = examId,
                        ClassId = classId,
                        SubjectId = s.SubjectId,
                        ExamDate = DateOnly.FromDateTime(s.ExamDate),
                        MaxMarks = s.MaxMarks
                    });
                }
                else
                {
                    existing.ExamDate = DateOnly.FromDateTime(s.ExamDate);
                    existing.MaxMarks = s.MaxMarks;
                }
            }

            await _context.SaveChangesAsync();

            var exam = await _context.Exams.FindAsync(examId);
            if (exam != null && exam.Status == "Draft")
            {
                exam.Status = "Scheduled";
                await _context.SaveChangesAsync();
            }

            return ApiResult<string>.Ok("OK", "Exam scheduled successfully");
        }
        public async Task<ApiResult<object>> GetSubjectsForMarksAsync(
    int examId,
    int classId)
        {
            var subjects = await (
                from es in _context.ExamSubjects
                join s in _context.Subjects on es.SubjectId equals s.SubjectId
                where es.ExamId == examId && es.ClassId == classId
                select new
                {
                    s.SubjectId,
                    s.SubjectName,
                    IsCompleted = _context.StudentMarks.Any(m =>
                        m.ExamId == examId &&
                        m.ClassId == classId &&
                        m.SubjectId == s.SubjectId)
                }
            ).ToListAsync();

            return ApiResult<object>.Ok(new { subjects });
        }


        public async Task<ApiResult<object>> CanDeclareResultAsync(int examId)
        {
            var totalSubjects = await _context.ExamSubjects
                .Where(x => x.ExamId == examId)
                .Select(x => new { x.ClassId, x.SubjectId })
                .Distinct()
                .CountAsync();

            var completedSubjects = await _context.StudentMarks
                .Where(x => x.ExamId == examId)
                .Select(x => new { x.ClassId, x.SubjectId })
                .Distinct()
                .CountAsync();

            return ApiResult<object>.Ok(new
            {
                canDeclare = totalSubjects > 0 && totalSubjects == completedSubjects
            });
        }


        public async Task<ApiResult<string>> DeclareResultAsync(int examId)
        {
            var exam = await _context.Exams.FindAsync(examId);

            if (exam == null)
                return ApiResult<string>.Fail("Exam not found");

            exam.Status = "Result Declared";
            await _context.SaveChangesAsync();

            return ApiResult<string>.Ok("OK", "Result declared successfully");
        }


        public async Task<object> GetStudentsForMarksAsync(
    int examId,
    int classId,
    int subjectId,
    int? streamId)
        {
            // 🔍 Validate that subject is part of exam schedule
            bool validExamSubject = await _context.ExamSubjects.AnyAsync(es =>
                es.ExamId == examId &&
                es.ClassId == classId &&
                es.SubjectId == subjectId);

            if (!validExamSubject)
                return new
                {
                    success = false,
                    message = "Subject not scheduled for this exam",
                    students = new List<object>()
                };

            // 👨‍🎓 Load students (stream filter only for class 11/12)
            var students = await _context.Students
                .Where(s =>
                    s.ClassId == classId &&
                    s.IsActive == true &&
                    (streamId == null || s.StreamId == streamId))
                .OrderBy(s => s.RollNo)
                .Select(s => new
                {
                    s.StudentId,
                    s.RollNo,
                    StudentName = s.FirstName + " " + s.LastName,

                    // 📝 Existing marks (if already entered)
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

            return new
            {
                success = true,
                examId,
                classId,
                subjectId,
                students
            };
        }
        public async Task<ApiResult<object>> GetSubjectsForScheduleAsync(
    int examId,
    int classId,
    int schoolId)
        {
            // 1️⃣ Validate exam
            var exam = await _context.Exams.FirstOrDefaultAsync(e =>
                e.ExamId == examId &&
                e.SchoolId == schoolId &&
                e.IsActive == true);

            if (exam == null)
                return ApiResult<object>.Fail("Exam not found");

            // 2️⃣ Validate exam-class mapping
            bool validClass = await _context.ExamClasses.AnyAsync(ec =>
                ec.ExamId == examId && ec.ClassId == classId);

            if (!validClass)
                return ApiResult<object>.Fail("Class not linked to exam");

            // 3️⃣ Load ALL class subjects
            var subjects = await _context.Subjects
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.IsActive == true)
                .Select(s => new
                {
                    subjectId = s.SubjectId,
                    subjectName = s.SubjectName,

                    // 👇 Try loading existing schedule if exists
                    examDate = _context.ExamSubjects
                        .Where(es =>
                            es.ExamId == examId &&
                            es.ClassId == classId &&
                            es.SubjectId == s.SubjectId)
                        .Select(es => es.ExamDate)
                        .FirstOrDefault(),

                    maxMarks = _context.ExamSubjects
                        .Where(es =>
                            es.ExamId == examId &&
                            es.ClassId == classId &&
                            es.SubjectId == s.SubjectId)
                        .Select(es => es.MaxMarks)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return ApiResult<object>.Ok(subjects);
        }




        // ---------------- ADD SUBJECTS ----------------

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
            // 1️⃣ Validate exam
            var exam = await _context.Exams
                .FirstOrDefaultAsync(e =>
                    e.ExamId == examId &&
                    e.SchoolId == schoolId &&
                    e.IsActive == true);

            if (exam == null)
                return ApiResult<object>.Fail("Exam not found");

            // 2️⃣ Validate exam ↔ class mapping
            bool validClass = await _context.ExamClasses.AnyAsync(ec =>
                ec.ExamId == examId &&
                ec.ClassId == classId);

            if (!validClass)
                return ApiResult<object>.Fail("Class not linked to this exam");

            // 3️⃣ Load ALL subjects for this class (school syllabus)
            var subjects = await _context.Subjects
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.IsActive == true)
                .Select(s => new
                {
                    subjectId = s.SubjectId,
                    subjectName = s.SubjectName
                })
                .ToListAsync();

            // 4️⃣ Load already assigned subjects (edit / prevent duplicate)
            var assignedSubjects = await _context.ExamSubjects
                .Where(es =>
                    es.ExamId == examId &&
                    es.ClassId == classId)
                .Select(es => new
                {
                    subjectId = es.SubjectId,
                    examDate = es.ExamDate,
                    maxMarks = es.MaxMarks
                })
                .ToListAsync();

            // 5️⃣ Final response
            return ApiResult<object>.Ok(new
            {
                examName = exam.ExamName,
                academicYear = exam.AcademicYear,
                subjects,                 // 👈 available to assign
                assignedSubjects          // 👈 already added (optional UI use)
            });
        }

public async Task<ApiResult<List<StreamDto>>> GetStreamsAsync(int schoolId, int classId)
    {
        // For classes below 11th, streams are not applicable
        if (classId < 11)
            return ApiResult<List<StreamDto>>.Ok(new List<StreamDto>());

        var streams = await _context.Streams
            .Where(s =>
                s.SchoolId == schoolId &&
                s.ClassId == classId &&
                s.IsActive == true
            )
            .Select(s => new StreamDto
            {
                StreamId = s.StreamId,
                StreamName = s.StreamName
            })
            .ToListAsync();

        // streams is NEVER null here (EF Core guarantee)
        return ApiResult<List<StreamDto>>.Ok(streams);
    }
        public async Task<ApiResult<string>> ScheduleExamAsync(ScheduleExamRequest req)
        {
            foreach (var s in req.Subjects)
            {
                var examSubject = await _context.ExamSubjects.FirstOrDefaultAsync(x =>
                    x.ExamId == req.ExamId &&
                    x.ClassId == req.ClassId &&
                    x.SubjectId == s.SubjectId
                );

                if (examSubject == null)
                    continue;

                examSubject.ExamDate = DateOnly.FromDateTime(s.ExamDate);
                examSubject.MaxMarks = s.MaxMarks;
            }

            await _context.SaveChangesAsync();

            // ✅ Update exam status
            var exam = await _context.Exams.FindAsync(req.ExamId);
            if (exam != null && exam.Status == "Subjects Assigned")
            {
                exam.Status = "Scheduled";
                await _context.SaveChangesAsync();
            }

            return ApiResult<string>.Ok("OK", "Exam scheduled successfully");
        }



    }
}

