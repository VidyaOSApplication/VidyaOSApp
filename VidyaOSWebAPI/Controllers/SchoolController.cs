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
        public async Task<IActionResult> ViewAttendance(
            [FromQuery] int schoolId,
            [FromQuery] int classId,
            [FromQuery] int sectionId,
            [FromQuery] DateTime date,
            [FromQuery] int? streamId // ✅ OPTIONAL
)
        {
            var result = await _schoolService.ViewAttendanceAsync(
                schoolId,
                classId,
                sectionId,
                DateOnly.FromDateTime(date),
                streamId
            );

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
        [HttpPost]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> TakeLeaveAction(
    LeaveActionRequest request)
        {
            var result = await _schoolService.TakeLeaveActionAsync(request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
        [HttpPost]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> SaveFeeStructure(
                                                        FeeStructureRequest request)
        {
            var result = await _schoolService.SaveFeeStructureAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        [HttpGet]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> GetFeeStructures(int schoolId)
        {
            var result = await _schoolService.GetFeeStructuresAsync(schoolId);
            return Ok(result);
        }
        [HttpPost]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> GenerateMonthlyFee(
    [FromBody] GenerateMonthlyFeeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FeeMonth))
                return BadRequest("Invalid request");

            var result = await _schoolService.GenerateMonthlyFeesAsync(
                request.SchoolId,
                request.FeeMonth   // ✅ string yyyy-MM
            );

            return result.Success ? Ok(result) : BadRequest(result);
        }


        // 4️⃣ Get Pending Fees (Admin Dashboard)
        [HttpGet]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> GetPendingFees(int schoolId)
        {
            var result = await _schoolService.GetPendingFeesAsync(schoolId);
            return Ok(result);
        }

        // 5️⃣ Collect / Pay Fee
        [HttpPost]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> CollectFees(CollectFeesRequest request)
        {
            var result = await _schoolService.CollectFeesAsync(request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // 6️⃣ Student Fee History
        [HttpGet]
        public async Task<IActionResult> GetStudentFeeHistory(int studentId)
        {
            var result = await _schoolService.GetStudentFeeHistoryAsync(studentId);
            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetFeeReceipt(
                    int studentId, string feeMonth)
        {
            var result = await _schoolService.GenerateFeeReceiptAsync(studentId, feeMonth);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        [HttpGet]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> GetStudentsByClassSection(
            int schoolId,
            int classId,
            int sectionId)
        {
            var result = await _schoolService.GetStudentsByClassSectionAsync(
                schoolId, classId, sectionId);

            return Ok(result);
        }
        [HttpGet]
        [Authorize(Roles = "SchoolAdmin,Teacher")]
        public async Task<IActionResult> GetTodaysBirthdays(int schoolId)
        {
            var result = await _schoolService.GetTodaysBirthdaysAsync(schoolId);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> GenerateRollNos(
                        GenerateRollNoRequest req)
        {
            var result = await _schoolService
                .GenerateRollNumbersAlphabeticallyAsync(
                    req.SchoolId, req.ClassId, req.SectionId);

            return result.Success ? Ok(result) : BadRequest(result);
        }

    }
}
