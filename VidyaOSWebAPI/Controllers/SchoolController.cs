using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSServices.Services;
 // <-- Change this import to use the DTO from the correct assembly

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
    }
}
