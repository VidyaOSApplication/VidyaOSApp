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
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(req.ExamName))
                    return ApiResult<int>.Fail("Exam name is required.");

                var exam = new Exam
                {
                    SchoolId = req.SchoolId,
                    ExamName = req.ExamName,
                    AcademicYear = req.AcademicYear,
                    // Convert DateTime to DateOnly for the Model
                    StartDate = req.StartDate.HasValue ? DateOnly.FromDateTime(req.StartDate.Value) : null,
                    EndDate = req.EndDate.HasValue ? DateOnly.FromDateTime(req.EndDate.Value) : null,
                    Status = "Upcoming", // Default status
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Exams.Add(exam);
                await _context.SaveChangesAsync();

                return ApiResult<int>.Ok(exam.ExamId, "Exam created successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception details here if needed
                return ApiResult<int>.Fail($"Failed to create exam: {ex.Message}");
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
                        ExamDate = DateOnly.FromDateTime((DateTime)s.ExamDate),
                        MaxMarks = (int)s.MaxMarks
                    });
                }
                else
                {
                    existing.ExamDate = DateOnly.FromDateTime((DateTime)s.ExamDate);
                    existing.MaxMarks = (int)s.MaxMarks;
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
            var subjects = await _context.Subjects
                .Where(s => s.ClassId == classId && s.IsActive==true)
                .Select(s => new
                {
                    subjectId = s.SubjectId,
                    subjectName = s.SubjectName,
                    isScheduled = _context.ExamSubjects.Any(es =>
                        es.ExamId == examId &&
                        es.ClassId == classId &&
                        es.SubjectId == s.SubjectId)
                })
                .ToListAsync();

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
            // ✅ Validate subject scheduled
            bool validExamSubject = await _context.ExamSubjects.AnyAsync(es =>
                es.ExamId == examId &&
                es.ClassId == classId &&
                es.SubjectId == subjectId);

            if (!validExamSubject)
                return new
                {
                    success = false,
                    message = "Subject not scheduled",
                    students = new List<object>()
                };

            var students = await (
                from s in _context.Students
                where s.ClassId == classId
                      && s.IsActive==true
                      && (streamId == null || s.StreamId == streamId)
                orderby s.RollNo
                let mark = _context.StudentMarks.FirstOrDefault(m =>
                    m.StudentId == s.StudentId &&
                    m.ExamId == examId &&
                    m.ClassId == classId &&
                    m.SubjectId == subjectId)
                select new
                {
                    studentId = s.StudentId,
                    rollNo = s.RollNo,
                    studentName = s.FirstName + " " + s.LastName,

                    // ✅ PREFILL LOGIC
                    marksObtained = mark != null ? mark.MarksObtained : (int?)null,
                    isAbsent = mark != null && mark.IsAbsent==true
                }
            ).ToListAsync();

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

            // 3️⃣ Load ALL class subjects with OPTIONAL schedule
            var subjects =
                from s in _context.Subjects
                where s.SchoolId == schoolId
                      && s.ClassId == classId
                      && s.IsActive==true
                join es in _context.ExamSubjects
                    .Where(x => x.ExamId == examId && x.ClassId == classId)
                    on s.SubjectId equals es.SubjectId into gj
                from es in gj.DefaultIfEmpty()
                select new
                {
                    subjectId = s.SubjectId,
                    subjectName = s.SubjectName,

                    // ✅ IMPORTANT FIX
                    examDate = es != null ? (DateOnly?)es.ExamDate : null,
                    maxMarks = es != null ? (int?)es.MaxMarks : null
                };

            return ApiResult<object>.Ok(await subjects.ToListAsync());
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
                // 🔥 ABSOLUTE FIX (prevents class 3 crash)
                if (!m.IsAbsent && m.MarksObtained == null)
                    continue;

                var existing = await _context.StudentMarks.FirstOrDefaultAsync(x =>
                    x.StudentId == m.StudentId &&
                    x.ExamId == req.ExamId &&
                    x.ClassId == req.ClassId &&
                    x.SubjectId == req.SubjectId);

                if (existing != null)
                {
                    existing.MarksObtained = (int)m.MarksObtained;
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
                        SubjectId = req.SubjectId,
                        MarksObtained = (int)m.MarksObtained,
                        IsAbsent = m.IsAbsent,
                        MaxMarks = examSubject.MaxMarks > 0 ? examSubject.MaxMarks : 100
                    });
                }
            }

            await _context.SaveChangesAsync();

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
        public async Task<ApiResult<object>> ScheduleExamAsync(
    ScheduleExamRequest request)
        {
            // 🔐 Validate exam
            var exam = await _context.Exams.FirstOrDefaultAsync(e =>
                e.ExamId == request.ExamId &&
                e.SchoolId == request.SchoolId &&
                e.IsActive==true);

            if (exam == null)
                return ApiResult<object>.Fail("Exam not found");

            foreach (var s in request.Subjects)
            {
                // 🔍 Check existing record
                var examSubject = await _context.ExamSubjects.FirstOrDefaultAsync(es =>
                    es.ExamId == request.ExamId &&
                    es.ClassId == request.ClassId &&
                    es.SubjectId == s.SubjectId);

                // 🗓️ Convert DateTime → DateOnly SAFELY
                var examDate = s.ExamDate.HasValue
                    ? DateOnly.FromDateTime(s.ExamDate.Value)
                    : DateOnly.FromDateTime(DateTime.Today);

                var maxMarks = s.MaxMarks ?? 100;

                if (examSubject == null)
                {
                    // ➕ INSERT
                    examSubject = new ExamSubject
                    {
                        ExamId = request.ExamId,
                        ClassId = request.ClassId,
                        SubjectId = s.SubjectId,
                        ExamDate = examDate,
                        MaxMarks = maxMarks
                    };

                    _context.ExamSubjects.Add(examSubject);
                }
                else
                {
                    // ✏️ UPDATE
                    examSubject.ExamDate = examDate;
                    examSubject.MaxMarks = maxMarks;
                }
            }

            await _context.SaveChangesAsync();
            var exam1 = await _context.Exams.FindAsync(request.ExamId);
            if (exam1 != null && exam1.Status == "Draft")
            {
                exam1.Status = "Scheduled";
            }

            await _context.SaveChangesAsync();

            return ApiResult<object>.Ok("Exam schedule saved");
        }
        

    }
}

