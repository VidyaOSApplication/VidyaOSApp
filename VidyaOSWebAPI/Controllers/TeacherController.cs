using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TeacherController : ControllerBase
    {
        private readonly TeacherService _teacherService;
        public TeacherController(TeacherService service)
        {
            _teacherService = service;
        }
        [HttpPost]
        public async Task<IActionResult> RegisterTeacher(
        RegisterTeacherRequest request)
        {
            var result = await _teacherService.RegisterTeacherAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents(
           [FromQuery] int schoolId,
           [FromQuery] int classId,
           [FromQuery] int sectionId,
           [FromQuery] int? streamId,
           [FromQuery] DateOnly date)
        {
            var result = await _teacherService
                .GetStudentsForAttendanceAsync(schoolId, classId, sectionId, date, streamId);

            return Ok(result);
        }

        // 2️⃣ SAVE attendance
        [Authorize(Roles = "Teacher")]
        [HttpPost("mark")]
        public async Task<IActionResult> MarkAttendance(
            [FromBody] AttendanceMarkRequest request)
        {
            await _teacherService.SaveAttendanceAsync(request);
            return Ok(new { success = true, message = "Attendance saved successfully." });
        }

        [HttpGet("Directory/{schoolId}")]
        public async Task<IActionResult> GetDirectory(int schoolId)
        {
            var result = await _teacherService.GetTeachersDirectoryAsync(schoolId);
            return Ok(result);
        }

        // GET: api/Teacher/Profile/5/1
        [HttpGet("{teacherId}/{schoolId}")]
        public async Task<IActionResult> GetProfile(int teacherId, int schoolId)
        {
            var result = await _teacherService.GetTeacherProfileAsync(teacherId, schoolId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST: api/Teacher/UpdateProfile
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] TeacherProfileDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload.");
            var result = await _teacherService.UpdateTeacherProfileAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
