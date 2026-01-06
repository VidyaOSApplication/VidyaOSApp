using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VidyaOSController : ControllerBase
    {
        private readonly StudentService _studentService;
        public VidyaOSController(StudentService service)
        {
            _studentService = service;
        }
    }
}
