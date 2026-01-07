using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SchoolController : ControllerBase
    {
        private readonly SchoolService _schoolService;
        public SchoolController(SchoolService service)
        {
            _schoolService = service;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStudent(
        StudentRegisterRequest request)
        {
            var result = await _schoolService.RegisterStudentAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result); // ✅ NO extra wrapping
        }
    }
    }
