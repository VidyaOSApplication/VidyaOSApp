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
    }
}
