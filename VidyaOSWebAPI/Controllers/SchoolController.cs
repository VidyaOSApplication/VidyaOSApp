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
        public async Task<IActionResult> RegisterSchool(RegisterSchoolRequest request)
        {
            try
            {
                await _schoolService.RegisterSchoolAsync(request);

                return Ok(new
                {
                    success = true,
                    message = "School registered successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPost]
        public async Task<IActionResult> RegisterStudent(StudentRegisterRequest request)
        {
            try
            {
                var result = await _schoolService.RegisterStudentAsync(request);

                return Ok(new
                {
                    success = true,
                    message = "Student registered successfully",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                // 🚫 Business conflict
                return Conflict(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                // 🔥 Unexpected error
                return StatusCode(500, new
                {
                    success = false,
                    message = "Something went wrong while registering student."
                });
            }
        }
        }
    }
