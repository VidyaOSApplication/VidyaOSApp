using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSServices.Services; 
namespace VidyaOS.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AIChatController : ControllerBase
    {
        private readonly AIChatService _aiService;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(AIChatService aiService, ILogger<AIChatController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<AIChatResponse>> Ask([FromBody] AIChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return Ok(AIChatResponse.Fail("Question cannot be empty."));

            if (request.SchoolId <= 0)
                return Ok(AIChatResponse.Fail("Invalid School ID."));

            try
            {
                _logger.LogInformation("AI Request: School {Id}, Question: {Q}", request.SchoolId, request.Question);

                var answer = await _aiService.AskAsync(request.SchoolId, request.Question);

                return Ok(AIChatResponse.Ok(answer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Error for School {Id}", request.SchoolId);
                return Ok(AIChatResponse.Fail("I'm having trouble thinking right now. Please try again."));
            }
        }
    }
}