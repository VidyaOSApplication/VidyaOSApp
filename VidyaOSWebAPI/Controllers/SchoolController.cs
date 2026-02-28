using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.DTOs.VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using VidyaOSServices.Services;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;
using QuestPDF.Fluent;
using VidyaOSHelper;

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
        public async Task<IActionResult> GetStudentFeeHistory(int schoolId,int studentId)
        {
            var result = await _schoolService.GetStudentFeeHistoryAsync(schoolId,studentId);
            return Ok(result);
        }

        [HttpGet("{feeId}")]
        public async Task<IActionResult> DownloadReceipt(int feeId)
        {
            try
            {
                // 🚀 Fetch data and generate PDF via Service
                var receiptData = await _schoolService.GetReceiptDataAsync(feeId);
                if (receiptData == null) return NotFound("Receipt data not found.");

                var pdfBytes = await _schoolService.GenerateFeeReceiptPdfAsync(feeId);

                // 🚀 Filename in IST: FirstName_Class_Section_ddMMyyyy.pdf
                string dateFormatted = receiptData.GenerationDateTime.ToString("ddMMyyyy");
                string studentFirstName = (receiptData.StudentName ?? "Student").Split(' ')[0];
                string className = receiptData.ClassName?.Replace(" ", "") ?? "Class";
                string sectionName = receiptData.SectionName?.Replace(" ", "") ?? "Sec";

                string fileName = $"{studentFirstName}_{className}_{sectionName}_{dateFormatted}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
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
            int sectionId,
            int? streamId = null)
        {
            var result = await _schoolService.GetStudentsByClassSectionAsync(
                schoolId, classId, sectionId,streamId );

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
        public async Task<IActionResult> GenerateRollNos([FromBody] GenerateRollNoRequest req)
        {
            // Pass the StreamId from the request to the service
            // If req.StreamId is null, the service uses its default parameter
            var result = await _schoolService.GenerateRollNumbersAlphabeticallyAsync(
                req.SchoolId,
                req.ClassId,
                req.SectionId,
                req.StreamId);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        
        [HttpGet]
        public async Task<IActionResult> GetSubjectsForClassSection(
            int schoolId,
            int classId,
            int? sectionId)
        {
            var result = await _schoolService
                .GetSubjectsForClassSectionAsync(
                    schoolId, classId, sectionId);

            return Ok(result);
        }

        // API 1: Called by StudentDirectory.tsx
        [HttpGet]
        public async Task<IActionResult> GetStudentsByFilters(int schoolId, int classId, int sectionId, int? streamId)
        {
            // The nullable int? streamId correctly handles both junior and senior classes
            var result = await _schoolService.GetStudentsByClassSectionAsync(schoolId, classId, sectionId, streamId);
            return Ok(result);
        }

        // API 2: Called by StudentProfile.tsx
        [HttpGet]
        public async Task<IActionResult> GetStudentDetails(int id)
        {
            var result = await _schoolService.GetStudentDetailsAsync(id);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateStudentDetails([FromBody] StudentDetailsDto dto)
        {
            if (dto.StudentId <= 0) return BadRequest(ApiResult<bool>.Fail("Invalid ID"));

            var result = await _schoolService.UpdateStudentDetailsAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetExams(int schoolId) => Ok(await _schoolService.GetExamsOnlyAsync(schoolId));

        [HttpGet]
        public async Task<IActionResult> GetSubjects(int schoolId, int classId, int? streamId) =>
            Ok(await _schoolService.GetSubjectsByContextAsync(schoolId, classId, streamId));

        [HttpGet]
        public async Task<IActionResult> GetMarksEntryList(int schoolId, int examId, int classId, int sectionId, int subjectId, int? streamId)
        {
            var result = await _schoolService.GetMarksEntryListAsync(schoolId, examId, classId, sectionId, subjectId, streamId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBulkMarks([FromBody] BulkSaveRequest request) =>
            Ok(await _schoolService.SaveBulkMarksAsync(request));

        [HttpGet]
        public async Task<IActionResult> GetStudentResultSummary(int studentId, int examId)
        {
            var result = await _schoolService.GetStudentResultSummaryAsync(studentId, examId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetHistory(int schoolId, int userId)
        {
            var result = await _schoolService.GetUserLeaveHistoryAsync(schoolId, userId);
            return Ok(result);
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> GetAttendanceHistory(
        [FromQuery] int userId,
        [FromQuery] int month,
        [FromQuery] int year)
        {
            if (userId <= 0 || month < 1 || month > 12)
                return BadRequest("Invalid parameters. Month must be 1-12.");

            var result = await _schoolService.GetStudentMonthlyAttendanceAsync(userId, month, year);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> GetFeeStatus([FromQuery] int schoolId, [FromQuery] int studentId)
        {
            if (schoolId <= 0 || studentId <= 0)
            {
                return BadRequest(ApiResult<List<FeeHistoryDto>>.Fail("Invalid School or Student ID."));
            }

            var result = await _schoolService.GetStudentFeeStatusAsync(schoolId, studentId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetDashboardSummary(int schoolId)
        {
            if (schoolId <= 0)
                return BadRequest(ApiResult<string>.Fail("Invalid School ID."));

            var result = await _schoolService.GetDashboardSummaryAsync(schoolId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddMaster([FromBody] MasterSubjectDto dto)
        {
            var result = await _schoolService.AddMasterSubjectAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMasterSubjects([FromQuery] int schoolId)
        {
            if (schoolId <= 0)
                return BadRequest(ApiResult<string>.Fail("Invalid School ID."));

            var result = await _schoolService.GetAllMasterSubjectsAsync(schoolId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMaster([FromBody] MasterSubjectDto dto)
        {
            if (dto.MasterSubjectId == null) return BadRequest("MasterSubjectId is required.");
            var result = await _schoolService.UpdateMasterSubjectAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> AssignToClass([FromBody] AssignSubjectDto dto)
        {
            var result = await _schoolService.AssignSubjectToClassAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignedSubjects([FromQuery] int schoolId, [FromQuery] int classId, [FromQuery] int? streamId)
    => Ok(await _schoolService.GetAssignedSubjectsAsync(schoolId, classId, streamId));

        [HttpDelete]
        public async Task<IActionResult> DeleteAssigned(int subjectId, int schoolId)
        {
            var result = await _schoolService.DeleteAssignedSubjectAsync(subjectId, schoolId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTimetableBulk([FromBody] TimetableBulkRequest request)
        {
            var result = await _schoolService.UpdateTimetableBulkAsync(request);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetTimetable(int schoolId, int classId, int sectionId, int? streamId)
        {
            var result = await _schoolService.GetTimetableAsync(schoolId, classId, sectionId, streamId);
            return Ok(result);
        }

        [HttpGet("{schoolId}/{classId}")]
        public async Task<IActionResult> GetStreams(int schoolId, int classId)
        {
            var result = await _schoolService.GetStreamsByClassAsync(schoolId, classId);
            return result.Success ? Ok(result.Data) : BadRequest(result.Message);
        }



    }
}
