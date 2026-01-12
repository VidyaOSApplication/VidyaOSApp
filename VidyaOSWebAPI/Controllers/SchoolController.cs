using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> RegisterSchool(
        VidyaOSDAL.DTOs.RegisterSchoolRequest request)
        {
            var result = await _schoolService.RegisterSchoolAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Teacher,SchoolAdmin")]
        public async Task<IActionResult> ViewAttendance(
            [FromQuery] int schoolId,
            [FromQuery] int classId,
            [FromQuery] int sectionId,
            [FromQuery] DateOnly date)
        {
            var result = await _schoolService.ViewAttendanceAsync(
                schoolId, classId, sectionId, date);

            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> ApplyLeave(ApplyLeaveRequest request)
        {
            var result = await _schoolService.ApplyLeaveAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // ADMIN
        [HttpGet]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> GetPendingLeaves(int schoolId)
        {
            var result = await _schoolService.GetPendingLeavesAsync(schoolId);
            return Ok(result);
        }

        // ADMIN
        [HttpPost]
        public async Task<IActionResult> UpdateLeaveStatus(
            int leaveId,
            string status,
            int adminUserId,
            string? remarks)
        {
            var result = await _schoolService.UpdateLeaveStatusAsync(
                leaveId, status, adminUserId, remarks);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
    }
