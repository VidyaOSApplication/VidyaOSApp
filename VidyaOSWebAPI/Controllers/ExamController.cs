using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        // 1️⃣ CREATE EXAM
        [HttpPost]
        public async Task<IActionResult> CreateExam(CreateExamRequest request)
        {
            var result = await _service.CreateExamAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 2️⃣ ADD SUBJECTS
        [HttpPost]
        public async Task<IActionResult> AddExamSubjects(AddExamSubjectsRequest request)
        {
            var result = await _service.AddExamSubjectsAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // 3️⃣ GET EXAM STRUCTURE
        [HttpGet]
        public async Task<IActionResult> GetExam(int examId)
        {
            var result = await _service.GetExamDetailsAsync(examId);
            return Ok(result);
        }

        // 4️⃣ SAVE MARKS
        //[HttpPost]
        //[Authorize(Roles = "Teacher")]
        //public async Task<IActionResult> SaveMarks(SaveStudentMarksRequest request)
        //{
        //    var result = await _service.SaveStudentMarksAsync(request);
        //    return result.Success ? Ok(result) : BadRequest(result);
        //}

        // 5️⃣ STUDENT RESULT
        [HttpGet]
        public async Task<IActionResult> GetStudentResult(int studentId, int examId)
        {
            var result = await _service.GetStudentResultAsync(studentId, examId);
            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetExams(int schoolId)
        {
            var result = await _service.GetExamsAsync(schoolId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetExamToAddSubjects(int examId, int classId,int schoolId)
        {
            // schoolId from JWT (same pattern as your other controllers)
            //int schoolId = int.Parse(User.FindFirst("schoolId")!.Value);

            var result = await _service.GetExamToAddSubjectsAsync(
                examId,
                classId,
                schoolId
            );

            return result.Success ? Ok(result) : BadRequest(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetAssignedSubjects(
    int examId,
    int classId,
    int schoolId)
        {
            var result = await _service.GetAssignedSubjectsAsync(
                examId, classId, schoolId);

            return result.Success ? Ok(result) : BadRequest(result);
        }


        [HttpGet]
        public async Task<IActionResult> GetStudentsForMarks(
            int examId,
            int classId,
            int subjectId)
        {
            var result = await _service.GetStudentsForMarksAsync(
                examId, classId, subjectId);

            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> SaveMarks([FromBody] SaveStudentMarksRequest request)
        {
            var result = await _service.SaveStudentMarksAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }


    }

}
