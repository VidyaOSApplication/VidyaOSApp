using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return BadRequest(new
                {
                    message = result.Message
                });

            return Ok(result);
        }

        [HttpGet]
        [Authorize] // Requires the token we just got
        public async Task<IActionResult> GetUserSession(int userId)
        {
            var result = await _authService.GetUserSessionAsync(userId);
            return Ok(result);
        }
    }
}
