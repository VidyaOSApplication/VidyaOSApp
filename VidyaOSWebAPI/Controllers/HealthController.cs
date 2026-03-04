using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        [HttpHead] // ⭐ add this
        public IActionResult GetHealth()
        {
            return Ok("healthy");
        }
    }
}
