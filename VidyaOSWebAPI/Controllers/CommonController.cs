using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly CommonService _service;
        public CommonController(CommonService service)
        {
            _service = service;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var profile = await _service.GetMyProfileAsync(User);
            return Ok(profile);
        }
    }
}
