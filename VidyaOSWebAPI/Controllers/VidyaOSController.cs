using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VidyaOSController : ControllerBase
    {
        private readonly VidyaOSService _vidyaOSService;
        public VidyaOSController(VidyaOSService service)
        {
            _vidyaOSService = service;
        }
        
    }
}
