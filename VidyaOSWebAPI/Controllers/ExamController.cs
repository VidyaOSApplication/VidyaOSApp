using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ExamController : ControllerBase
    {
        private readonly ExamService _service;

        public ExamController(ExamService service)
        {
            _service = service;
        }

        // =========================================================
        // 1️⃣ CREATE EXAM
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateExam(CreateExamRequest request)
        {
            var result = await _service.CreateExamAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // 2️⃣ LIST EXAMS (Exam Dashboard / Exam List)
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher,Student")]
        [HttpGet]
        public async Task<IActionResult> GetExams(int schoolId)
        {
            var result = await _service.GetExamsAsync(schoolId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // 3️⃣ GET SUBJECTS FOR SCHEDULING EXAM
        // (AUTO syllabus subjects – no manual selection)
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpGet]
        
        public async Task<IActionResult> GetScheduleSubjects(
            int examId,
            int classId,
            int schoolId)
        {
            var result = await _service.GetScheduleSubjectsAsync(
                examId, classId, schoolId);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // 4️⃣ SAVE EXAM SCHEDULE (DATE + MAX MARKS)
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpPost]

        public async Task<IActionResult> SaveExamSchedule(
            int examId,
            int classId,
            [FromBody] List<ScheduleSubjectDto> subjects)
        {
            var result = await _service.SaveExamScheduleAsync(
                examId, classId, subjects);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // 5️⃣ SUBJECT LIST FOR ENTER MARKS
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpGet]
        public async Task<IActionResult> GetSubjectsForMarks(
            int examId,
            int classId)
        {
            var result = await _service.GetSubjectsForMarksAsync(
                examId, classId);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // 6️⃣ STUDENTS FOR ENTER MARKS (OPTIONAL STREAM)
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpGet]
        public async Task<IActionResult> GetStudentsForMarks(
            int examId,
            int classId,
            int subjectId,
            int? streamId)
        {
            var result = await _service.GetStudentsForMarksAsync(
                examId, classId, subjectId, streamId);

            return Ok(result);
        }

        // =========================================================
        // 7️⃣ SAVE STUDENT MARKS
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> SaveMarks(
            [FromBody] SaveStudentMarksRequest request)
        {
            var result = await _service.SaveStudentMarksAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // 8️⃣ CHECK IF RESULT CAN BE DECLARED
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpGet]
        public async Task<IActionResult> CanDeclareResult(int examId)
        {
            var result = await _service.CanDeclareResultAsync(examId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =========================================================
        // 9️⃣ DECLARE RESULT
        // =========================================================
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> DeclareResult(int examId)
        {
            var result = await _service.DeclareResultAsync(examId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpGet]
        public async Task<IActionResult> GetSubjectsForSchedule(
                    int examId,
                    int classId)
        {
            int schoolId = int.Parse(User.FindFirst("schoolId")!.Value);

            var result = await _service.GetSubjectsForScheduleAsync(
                examId, classId, schoolId);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "SchoolAdmin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> ScheduleExam([FromBody] ScheduleExamRequest request)
        {
            var result = await _service.ScheduleExamAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }


    }
}
