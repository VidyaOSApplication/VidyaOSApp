using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SuperAdminController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;

        public SuperAdminController(SubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // GET: api/SuperAdmin/GetAvailablePlans
        [HttpGet]
        public async Task<IActionResult> GetAvailablePlans()
        {
            var result = await _subscriptionService.GetAvailablePlansAsync();
            return Ok(result);
        }

        // POST: api/SuperAdmin/ActivateSubscription
        [HttpPost]
        public async Task<IActionResult> ActivateSubscription([FromBody] ActivateSubRequest req)
        {
            var result = await _subscriptionService.ManuallyActivateSubscriptionAsync(req.SchoolId, req.PlanId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllSchools()
        {
            // Calls the service to get all schools registered in VidyaOS
            var result = await _subscriptionService.GetAllSchoolsAsync();

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }

   
}

