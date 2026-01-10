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
           [FromQuery] DateOnly date)
        {
            var result = await _teacherService
                .GetStudentsForAttendanceAsync(schoolId, classId, sectionId, date);

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
    }
}
