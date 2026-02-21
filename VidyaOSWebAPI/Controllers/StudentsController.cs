using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly StudentService _studentService;
        public StudentsController(StudentService service)
        {
           _studentService = service;
        }
        [HttpGet]
        public IActionResult GetStudentsForAttendence()
        {
            var students = _studentService.GetAllStudents();
            if (students != null && students.Count > 0)
            {
                return Ok(students);
            }
            return NotFound("No students found");
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStudent(
        StudentRegisterRequest request)
        {
            var result = await _studentService.RegisterStudentAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result); // ✅ NO extra wrapping
        }

    }
}
