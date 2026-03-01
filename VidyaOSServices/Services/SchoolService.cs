using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.DTOs.VidyaOS.Models.DTOs;
using VidyaOSDAL.DTOs.VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using VidyaOSHelper.SchoolHelper;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;




namespace VidyaOSServices.Services
{
    public class SchoolService
    {
        private readonly VidyaOsContext _context;
        private readonly VidyaOSHelper.SchoolHelper.SchoolHelper _schoolHelper;
        public SchoolService(VidyaOsContext context, SchoolHelper schoolHelper)
        {
            _context = context;
            _schoolHelper = schoolHelper;
        }
        public async Task<ApiResult<RegisterSchoolResponse>> RegisterSchoolAsync(RegisterSchoolRequest req)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. ---------- ESSENTIAL VALIDATION ----------
                if (string.IsNullOrWhiteSpace(req.SchoolName))
                    return ApiResult<RegisterSchoolResponse>.Fail("School name is required.");

                if (string.IsNullOrWhiteSpace(req.AdminUsername))
                    return ApiResult<RegisterSchoolResponse>.Fail("Admin username is required.");

                // ---------- FIXED SCHOOL CODE GENERATION ----------
                var schoolCode = "";
                if (string.IsNullOrWhiteSpace(req.SchoolCode))
                {
                    // Split by space, filter out empty strings
                    var words = req.SchoolName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Extract the first character of each word, ensuring it's a valid character
                    var initials = string.Concat(words.Select(w => w[0])).ToUpper();

                }
                else
                {
                    schoolCode = req.SchoolCode.Trim().ToUpper();
                }


                // 2. ---------- CREATE SCHOOL PROFILE ----------
                var school = new School
                {
                    SchoolName = req.SchoolName.Trim(),
                    SchoolCode = schoolCode,
                    BoardType = req.BoardType,
                    Phone = req.Phone,
                    Email = req.Email,
                    City = req.City,
                    State = req.State,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Schools.Add(school);
                await _context.SaveChangesAsync(); // Generates SchoolId

                // 3. ---------- AUTO-SEED INFRASTRUCTURE ----------
                var classDefs = new List<(int Id, string Name)>
        {
            (15, "Nursery"), (13, "LKG"), (14, "UKG"),
            (1, "Class 1"), (2, "Class 2"), (3, "Class 3"), (4, "Class 4"),
            (5, "Class 5"), (6, "Class 6"), (7, "Class 7"), (8, "Class 8"),
            (9, "Class 9"), (10, "Class 10"), (11, "Class 11"), (12, "Class 12")
        };

                foreach (var cls in classDefs)
                {
                    // A. Create Class
                    _context.Classes.Add(new Class
                    {
                        SchoolId = school.SchoolId,
                        ClassId = cls.Id,
                        ClassName = cls.Name,
                        IsActive = true
                    });

                    // B. Create Sections A (ID 1) and B (ID 2)
                    _context.Sections.AddRange(
                        new Section { SchoolId = school.SchoolId, ClassId = cls.Id, SectionId = 1, SectionName = "A", IsActive = true },
                        new Section { SchoolId = school.SchoolId, ClassId = cls.Id, SectionId = 2, SectionName = "B", IsActive = true }
                    );

                    // C. Create Streams for Class 11 and 12
                    if (cls.Id == 11 || cls.Id == 12)
{
    _context.Streams.AddRange(
        new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "PCM", IsActive = true },
        new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "PCB", IsActive = true },
        new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "Commerce", IsActive = true },
        new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "Arts", IsActive = true }
    );
}
                }

                // 4. ---------- CREATE ADMIN USER ----------
                var adminUser = new User
                {
                    SchoolId = school.SchoolId,
                    Username = req.AdminUsername.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.AdminPassword),
                    Role = "SchoolAdmin",
                    Phone = req.Phone,
                    Email = req.Email,
                    IsFirstLogin = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return ApiResult<RegisterSchoolResponse>.Ok(new RegisterSchoolResponse
                {
                    SchoolId = school.SchoolId,
                    SchoolName = school.SchoolName,
                    AdminUsername = adminUser.Username
                }, "School registered and classroom environment initialized.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ApiResult<RegisterSchoolResponse>.Fail($"Registration failed: {ex.Message}");
            }
        }

        public async Task<AttendanceViewResponse> ViewAttendanceAsync(
    int schoolId,
    int classId,
    int sectionId,
    DateOnly date,
    int? streamId // ✅ NEW (OPTIONAL)
)
        {
            // 1️⃣ Students of class + section (+ stream only for 11/12)
            var studentsQuery = _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.SectionId == sectionId &&
                    s.IsActive == true);

            // ✅ APPLY STREAM FILTER ONLY FOR 11 & 12
            if ((classId == 11 || classId == 12) && streamId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.StreamId == streamId);
            }

            var students = await studentsQuery
                .OrderBy(s => s.RollNo)
                .Select(s => new
                {
                    s.UserId,
                    s.RollNo,
                    s.AdmissionNo,
                    FullName = s.FirstName + " " + s.LastName
                })
                .ToListAsync();

            if (!students.Any())
            {
                return new AttendanceViewResponse
                {
                    Success = false,
                    Message = "No students found for selected filters",
                    AttendanceDate = date
                };
            }

            var userIds = students.Select(s => s.UserId).ToList();

            // 2️⃣ Approved leaves
            var leaveUserIds = await _context.Leaves
                .Where(l =>
                    l.SchoolId == schoolId &&
                    l.Status == "Approved" &&
                    date >= l.FromDate &&
                    date <= l.ToDate)
                .Select(l => l.UserId)
                .ToListAsync();

            // 3️⃣ Attendance records
            var attendance = await _context.Attendances
                .Where(a =>
                    a.SchoolId == schoolId &&
                    a.AttendanceDate == date &&
                    userIds.Contains(a.UserId))
                .ToListAsync();

            bool attendanceTaken = attendance.Any();

            int present = 0, absent = 0, leave = 0, notMarked = 0;

            var result = students.Select(s =>
            {
                if (leaveUserIds.Contains(s.UserId))
                {
                    leave++;
                    return new AttendanceViewStudentDto
                    {
                        RollNo = (int)s.RollNo,
                        AdmissionNo = s.AdmissionNo!,
                        FullName = s.FullName,
                        Status = "Leave"
                    };
                }

                var att = attendance.FirstOrDefault(a => a.UserId == s.UserId);

                if (att == null)
                {
                    notMarked++;
                    return new AttendanceViewStudentDto
                    {
                        RollNo = (int)s.RollNo,
                        AdmissionNo = s.AdmissionNo!,
                        FullName = s.FullName,
                        Status = "NotMarked"
                    };
                }

                if (att.Status == "Present")
                    present++;
                else
                    absent++;

                return new AttendanceViewStudentDto
                {
                    RollNo = (int)s.RollNo,
                    AdmissionNo = s.AdmissionNo!,
                    FullName = s.FullName,
                    Status = att.Status!
                };
            }).ToList();

            return new AttendanceViewResponse
            {
                AttendanceDate = date,
                AttendanceTaken = attendanceTaken,
                Summary = new AttendanceSummary
                {
                    Total = students.Count,
                    Present = present,
                    Absent = absent,
                    Leave = leave,
                    NotMarked = notMarked
                },
                Students = result
            };
        }
        public async Task<ApiResult<LeaveResponse>> ApplyLeaveAsync(ApplyLeaveRequest req)
        {
            if (req == null)
                return ApiResult<LeaveResponse>.Fail("Request is required.");

            // Validation: Prevent past dates or logic errors
            if (req.FromDate.Date > req.ToDate.Date)
                return ApiResult<LeaveResponse>.Fail("From date cannot be greater than To date.");

            var fromDate = DateOnly.FromDateTime(req.FromDate);
            var toDate = DateOnly.FromDateTime(req.ToDate);

            bool isUpdated = false;

            // 🔍 Check for overlapping PENDING leave for this specific User (Teacher or Student)
            var existingLeave = await _context.Leaves
                .FirstOrDefaultAsync(l =>
                    l.SchoolId == req.SchoolId &&
                    l.UserId == req.UserId && // standardized to UserId
                    l.Status == "Pending" &&
                    l.FromDate <= toDate &&
                    l.ToDate >= fromDate
                );

            LeaveRequest targetLeave;

            if (existingLeave != null)
            {
                // 🔁 UPDATE: Modify the existing pending request
                existingLeave.FromDate = fromDate;
                existingLeave.ToDate = toDate;
                existingLeave.Reason = req.Reason;
                existingLeave.AppliedOn = DateOnly.FromDateTime(DateTime.UtcNow);

                targetLeave = existingLeave;
                isUpdated = true;
            }
            else
            {
                // ➕ CREATE: New leave entry
                targetLeave = new LeaveRequest
                {
                    SchoolId = req.SchoolId,
                    UserId = req.UserId, // standardized to UserId
                    FromDate = fromDate,
                    ToDate = toDate,
                    Reason = req.Reason,
                    Status = "Pending",
                    AppliedOn = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                _context.Leaves.Add(targetLeave);
            }

            try
            {
                await _context.SaveChangesAsync();

                return ApiResult<LeaveResponse>.Ok(
                    new LeaveResponse
                    {
                        LeaveId = targetLeave.LeaveId,
                        Status = targetLeave.Status!,
                        AppliedAt = targetLeave.AppliedOn ?? DateOnly.FromDateTime(DateTime.UtcNow)
                    },
                    isUpdated ? "Leave updated successfully." : "Leave applied successfully."
                );
            }
            catch (Exception ex)
            {
                return ApiResult<LeaveResponse>.Fail("An error occurred while saving: " + ex.Message);
            }
        }

        // ADMIN: APPROVE / REJECT LEAVE
        public async Task<ApiResult<string>> UpdateLeaveStatusAsync(
            int leaveId,
            string status,
            int adminUserId,
            string? remarks)
        {
            if (status != "Approved" && status != "Rejected")
                return ApiResult<string>.Fail("Invalid status.");

            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l => l.LeaveId == leaveId);

            if (leave == null)
                return ApiResult<string>.Fail("Leave not found.");

            leave.Status = status;
            leave.ApprovedBy = adminUserId;
            leave.ApprovedOn = DateOnly.FromDateTime(DateTime.UtcNow);
            await _context.SaveChangesAsync();

            return ApiResult<string>.Ok(
                status,
                $"Leave {status.ToLower()} successfully."
            );
        }

        public async Task<ApiResult<List<PendingLeaveDto>>> GetPendingLeavesAsync(int schoolId)
        {
            var result = await (
                from l in _context.Leaves
                join u in _context.Users on l.UserId equals u.UserId
                where l.SchoolId == schoolId && l.Status == "Pending"
                select new { l, u }
            ).ToListAsync();

            var response = new List<PendingLeaveDto>();

            foreach (var item in result)
            {
                string name = "";
                int? classId = null;
                int? sectionId = null;

                if (item.u.Role == "Student")
                {
                    var student = await (
                        from s in _context.Students
                        where s.UserId == item.u.UserId
                        select new
                        {
                            Name = s.FirstName + " " + s.LastName,
                            classId = s.ClassId,
                            sectionId = s.SectionId
                        }
                    ).FirstOrDefaultAsync();

                    if (student != null)
                    {
                        name = student.Name;
                        classId = student.classId;
                        sectionId = student.sectionId;
                    }
                }
                else if (item.u.Role == "Teacher")
                {
                    name = await _context.Teachers
                        .Where(t => t.UserId == item.u.UserId)
                        .Select(t => t.FullName)
                        .FirstOrDefaultAsync() ?? "Teacher";
                }

                response.Add(new PendingLeaveDto
                {
                    LeaveId = item.l.LeaveId,
                    UserId = item.u.UserId,
                    Role = item.u.Role!,
                    Name = name,
                    ClassId = classId,
                    SectionId = sectionId,
                    FromDate = (DateOnly)item.l.FromDate,
                    ToDate = (DateOnly)item.l.ToDate,
                    Reason = item.l.Reason ?? "",
                    Status = item.l.Status!,
                    AppliedOn = item.l.AppliedOn!.Value
                });
            }

            return ApiResult<List<PendingLeaveDto>>.Ok(response);
        }
        public async Task<ApiResult<string>> TakeLeaveActionAsync(
    LeaveActionRequest req)
        {
            if (req == null)
                return ApiResult<string>.Fail("Request is required.");

            if (req.Action != "Approved" && req.Action != "Rejected")
                return ApiResult<string>.Fail("Invalid action.");

            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l => l.LeaveId == req.LeaveId);

            if (leave == null)
                return ApiResult<string>.Fail("Leave request not found.");

            if (leave.Status != "Pending")
                return ApiResult<string>.Fail(
                    $"Leave already {leave.Status?.ToLower()}."
                );

            leave.Status = req.Action;
            leave.ApprovedBy = req.AdminUserId;
            leave.ApprovedOn = DateOnly.FromDateTime(DateTime.UtcNow);

            await _context.SaveChangesAsync();

            return ApiResult<string>.Ok(
                req.Action,
                $"Leave {req.Action.ToLower()} successfully."
            );
        }
        public async Task<ApiResult<string>> GenerateMonthlyFeesAsync(int schoolId, string feeMonth)
        {
            if (schoolId <= 0 || string.IsNullOrWhiteSpace(feeMonth))
                return ApiResult<string>.Fail("Invalid input.");

            // 1️⃣ Fetch all active students for THIS school
            var students = await _context.Students
                .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                .ToListAsync();

            if (!students.Any())
                return ApiResult<string>.Fail("No active students found for this school.");

            // 2️⃣ Check for existing fees for this month and school to prevent double-generation
            // 🚀 Added SchoolId filter here for strict isolation
            var existingFeeStudentIds = await _context.StudentFees
                    .Where(f => f.SchoolId == schoolId && f.FeeMonth == feeMonth)
                    .Select(f => (int)f.StudentId)
                    .ToListAsync();

            // 3️⃣ Fetch all active fee structures for THIS school
            var feeStructures = await _context.FeeStructures
                .Where(f => f.SchoolId == schoolId && f.IsActive == true)
                .ToListAsync();

            var newFees = new List<StudentFee>();

            foreach (var student in students)
            {
                // Skip if fee already exists for this specific student in this specific month
                if (existingFeeStudentIds.Contains(student.StudentId))
                    continue;

                // 4️⃣ Match the correct fee structure (Senior Class Logic: Class + Stream)
                bool isSenior = (student.ClassId == 11 || student.ClassId == 12);

                var structure = feeStructures.FirstOrDefault(f =>
                    f.ClassId == student.ClassId &&
                    (!isSenior || f.StreamId == student.StreamId)
                );

                if (structure != null)
                {
                    newFees.Add(new StudentFee
                    {
                        SchoolId = schoolId, // 🚀 CRITICAL: Mapping the SchoolId for data isolation
                        StudentId = student.StudentId,
                        FeeMonth = feeMonth,
                        Amount = structure.MonthlyAmount,
                        Status = "Pending",
                    });
                }
            }

            // 5️⃣ Bulk Insert
            if (newFees.Any())
            {
                _context.StudentFees.AddRange(newFees);
                await _context.SaveChangesAsync();
                return ApiResult<string>.Ok($"{newFees.Count} fee records generated successfully for {feeMonth}.");
            }

            return ApiResult<string>.Ok("No new fees were generated. Records might already exist for all students.");
        }


        public async Task<ApiResult<List<PendingFeeResponse>>> GetPendingFeesAsync(int schoolId)
        {
            // 1. Get all valid Student IDs for this school first
            var schoolStudentIds = await _context.Students
                .Where(s => s.SchoolId == schoolId)
                .Select(s => s.StudentId)
                .ToListAsync();

            if (!schoolStudentIds.Any())
            {
                return ApiResult<List<PendingFeeResponse>>.Ok(new List<PendingFeeResponse>());
            }

            // 2. Fetch fees only for these specific students
            // This prevents the 180-record duplication by isolating the student pool
            var data = await (
                from sf in _context.StudentFees
                join s in _context.Students on sf.StudentId equals s.StudentId
                join c in _context.Classes on s.ClassId equals c.ClassId
                join sec in _context.Sections on s.SectionId equals sec.SectionId
                where schoolStudentIds.Contains((int)sf.StudentId) // 🚀 The key filter
                   && sf.Status == "Pending"
                select new PendingFeeResponse
                {
                    StudentFeeId = sf.StudentFeeId,
                    StudentId = s.StudentId,
                    StudentName = s.FirstName + " " + s.LastName,
                    AdmissionNo = s.AdmissionNo!,
                    ClassName = c.ClassName!,
                    SectionName = sec.SectionName!,
                    FeeMonth = sf.FeeMonth!,
                    Amount = sf.Amount ?? 0
                }
            )
            .Distinct() // 🛡️ Safety net for any remaining configuration duplicates
            .ToListAsync();

            return ApiResult<List<PendingFeeResponse>>.Ok(data);
        }
        public async Task<ApiResult<bool>> CollectFeesAsync(CollectFeesRequest req)
        {
            if (req.StudentId <= 0 || !req.FeeMonths.Any()) return ApiResult<bool>.Fail("Invalid request.");

            var fees = await _context.StudentFees
                .Where(f => f.SchoolId == req.SchoolId && f.StudentId == req.StudentId && req.FeeMonths.Contains(f.FeeMonth!) && f.Status == "Pending")
                .ToListAsync();

            if (!fees.Any()) return ApiResult<bool>.Fail("No pending fees found.");

            foreach (var fee in fees)
            {
                fee.Status = "Paid";
                fee.PaymentMode = req.PaymentMode;
                fee.PaidOn = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            try
            {
                await _context.SaveChangesAsync();
                return ApiResult<bool>.Ok(true, "Fees collected successfully.");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail("Database error: " + ex.Message);
            }
        }

        public async Task<byte[]> GenerateFeeReceiptPdfAsync(int feeId)
        {
            // 🚀 Fetch the fully populated DTO
            var data = await GetReceiptDataAsync(feeId);
            if (data == null) throw new Exception("Receipt data not found");

            // 🚀 Generate PDF using the DTO
            var document = new FeeReceiptDocument(data);
            return document.GeneratePdf();
        }

        public async Task<FeeReceiptDto?> GetReceiptDataAsync(int studentFeeId)
        {
            // 🚀 Calculate IST Time for display
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

            // 🚀 High-performance JOIN fetching data from 5 tables in one query
            var data = await (from f in _context.StudentFees
                              join s in _context.Students on f.StudentId equals s.StudentId
                              join sch in _context.Schools on f.SchoolId equals sch.SchoolId
                              join cls in _context.Classes on s.ClassId equals cls.ClassId
                              join sec in _context.Sections on s.SectionId equals sec.SectionId
                              where f.StudentFeeId == studentFeeId && f.Status == "Paid"
                              select new FeeReceiptDto
                              {
                                  StudentFeeId = f.StudentFeeId,
                                  TotalAmount = f.Amount ?? 0,
                                  PaymentMode = f.PaymentMode ?? "Cash",
                                  PaidOn = f.PaidOn,
                                  GenerationDateTime = istNow,

                                  // 🚀 Fixed: Pulling academic session and admission info from Students table
                                  AcademicYear = s.AcademicYear ?? "2025-26",
                                  AdmissionNo = s.AdmissionNo ?? "N/A",
                                  RollNo = s.RollNo,
                                  StudentName = $"{s.FirstName} {s.LastName}",

                                  SchoolName = sch.SchoolName,
                                  SchoolAddress = $"{sch.AddressLine1}, {sch.City}",
                                  SchoolCode = sch.SchoolCode,
                                  SchoolEmail = sch.Email,
                                  SchoolPhone = sch.Phone,
                                  AffiliationNo = sch.AffiliationNumber,

                                  ClassName = cls.ClassName,
                                  SectionName = sec.SectionName,
                                  FeeMonth = f.FeeMonth // Raw format: "2026-6"
                              }).FirstOrDefaultAsync();

            if (data == null) return null;

            // 🚀 ENHANCEMENT: Format "2026-6" to "June 2026"
            if (!string.IsNullOrEmpty(data.FeeMonth) && data.FeeMonth.Contains("-"))
            {
                var parts = data.FeeMonth.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int year) && int.TryParse(parts[1], out int month))
                {
                    data.FeeMonth = new DateTime(year, month, 1).ToString("MMMM yyyy");
                }
            }

            // 🚀 Generate a professional Receipt Number
            data.ReceiptNo = $"{data.SchoolCode?.ToUpper()}/{data.GenerationDateTime:yyyy}/{data.StudentFeeId:D4}";

            return data;
        }


        public async Task<ApiResult<FeeStructureResponse>> SaveFeeStructureAsync(FeeStructureRequest req)
        {
            // 1. BASIC VALIDATIONS
            if (req == null) return ApiResult<FeeStructureResponse>.Fail("Request is required.");
            if (req.SchoolId <= 0) return ApiResult<FeeStructureResponse>.Fail("Invalid school.");
            if (req.ClassId <= 0) return ApiResult<FeeStructureResponse>.Fail("Invalid class.");
            if (string.IsNullOrWhiteSpace(req.FeeName)) return ApiResult<FeeStructureResponse>.Fail("Fee name is required.");
            if (req.MonthlyAmount <= 0) return ApiResult<FeeStructureResponse>.Fail("Monthly amount must be greater than zero.");

            // 2. UPDATED STREAM RULE 
            // Only 11 and 12 require a stream. All others (1-10, 13, 14, 15) must be null.
            bool isSeniorClass = (req.ClassId == 11 || req.ClassId == 12);

            if (isSeniorClass && req.StreamId == null)
                return ApiResult<FeeStructureResponse>.Fail("Stream is required for class 11 and 12.");

            if (!isSeniorClass)
                req.StreamId = null; // Forces Nursery, LKG, UKG, and 1-10 to have no stream

            // 3. CHECK EXISTING FEE
            // We use a specific check for null to ensure EF generates the correct 'IS NULL' SQL
            var existingFee = await _context.FeeStructures
                .FirstOrDefaultAsync(f =>
                    f.SchoolId == req.SchoolId &&
                    f.ClassId == req.ClassId &&
                    f.StreamId == req.StreamId &&
                    f.IsActive == true
                );

            // 4. UPDATE
            if (existingFee != null)
            {
                existingFee.FeeName = req.FeeName;
                existingFee.MonthlyAmount = req.MonthlyAmount;
                // Optionally update UpdatedAt if you have that column

                await _context.SaveChangesAsync();

                return ApiResult<FeeStructureResponse>.Ok(MapToResponse(existingFee), "Fee structure updated successfully.");
            }

            // 5. CREATE NEW
            var fee = new FeeStructure
            {
                SchoolId = req.SchoolId,
                ClassId = req.ClassId,
                StreamId = req.StreamId,
                FeeName = req.FeeName,
                MonthlyAmount = req.MonthlyAmount,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.FeeStructures.Add(fee);
            await _context.SaveChangesAsync();

            return ApiResult<FeeStructureResponse>.Ok(MapToResponse(fee), "Fee structure created successfully.");
        }

        // Private helper to avoid repeating the mapping code
        private FeeStructureResponse MapToResponse(FeeStructure f)
        {
            return new FeeStructureResponse
            {
                FeeStructureId = f.FeeStructureId,
                ClassId = f.ClassId!.Value,
                StreamId = f.StreamId,
                FeeName = f.FeeName!,
                MonthlyAmount = f.MonthlyAmount!.Value,
                IsActive = f.IsActive ?? true
            };
        }


        public async Task<ApiResult<List<FeeStructureListResponse>>> GetFeeStructuresAsync(int schoolId)
        {
            var query = await _context.FeeStructures
                .Where(fs => fs.SchoolId == schoolId && fs.IsActive == true)
                .Select(fs => new FeeStructureListResponse
                {
                    FeeStructureId = fs.FeeStructureId,
                    ClassId = (int)fs.ClassId!,
                    StreamId = fs.StreamId,
                    FeeName = fs.FeeName!,
                    MonthlyAmount = (decimal)fs.MonthlyAmount!,
                    IsActive = fs.IsActive ?? false,
                    // 🚀 Sub-queries instead of flat joins prevent row multiplication
                    ClassName = _context.Classes
                        .Where(c => c.ClassId == fs.ClassId)
                        .Select(c => c.ClassName)
                        .FirstOrDefault() ?? "Class " + fs.ClassId,
                    StreamName = _context.Streams
                        .Where(s => s.StreamId == fs.StreamId)
                        .Select(s => s.StreamName)
                        .FirstOrDefault() ?? ""
                })
                .OrderBy(x => x.ClassId)
                .ToListAsync();

            return ApiResult<List<FeeStructureListResponse>>.Ok(query);
        }
        public async Task<ApiResult<List<StudentFeeHistoryResponse>>> GetStudentFeeHistoryAsync(int schoolId, int studentId)
        {
            // 1. Validation
            if (schoolId <= 0)
                return ApiResult<List<StudentFeeHistoryResponse>>.Fail("Invalid school id.");

            if (studentId <= 0)
                return ApiResult<List<StudentFeeHistoryResponse>>.Fail("Invalid student id.");

            // 2. Fetch fees filtered by both School and Student
            // This ensures data isolation between schools
            var fees = await _context.StudentFees
                .Where(f => f.SchoolId == schoolId && f.StudentId == studentId)
                .OrderByDescending(f => f.FeeMonth) // Note: Consider sorting by a date if FeeMonth is just a string
                .Select(f => new StudentFeeHistoryResponse
                {
                    StudentFeeId = f.StudentFeeId,
                    FeeMonth = f.FeeMonth ?? "",
                    Amount = f.Amount ?? 0,
                    Status = f.Status ?? "Pending",
                    PaymentMode = f.PaymentMode,
                    PaidOn = f.PaidOn
                })
                .ToListAsync();

            // 3. Optional: Double check if student exists in this school if no fees are found
            if (!fees.Any())
            {
                bool studentExists = await _context.Students
                    .AnyAsync(s => s.StudentId == studentId && s.SchoolId == schoolId);

                if (!studentExists)
                    return ApiResult<List<StudentFeeHistoryResponse>>.Fail("Student not found in this school.");
            }

            return ApiResult<List<StudentFeeHistoryResponse>>.Ok(fees);
        }


        public async Task<ApiResult<FeeReceiptResponse>> GenerateFeeReceiptAsync(int studentId, string feeMonth)
{
    // 🚀 Join Students to get AcademicYear
    var feeData = await (from f in _context.StudentFees
                         join s in _context.Students on f.StudentId equals s.StudentId
                         join sch in _context.Schools on f.SchoolId equals sch.SchoolId
                         join cls in _context.Classes on s.ClassId equals cls.ClassId
                         join sec in _context.Sections on s.SectionId equals sec.SectionId
                         where f.StudentId == studentId && f.FeeMonth == feeMonth && f.Status == "Paid"
                         select new {
                             Fee = f,
                             Student = s,
                             School = sch,
                             ClassName = cls.ClassName,
                             SectionName = sec.SectionName
                         }).FirstOrDefaultAsync();

    if (feeData == null)
        return ApiResult<FeeReceiptResponse>.Fail("Paid fee record not found.");

    // 🚀 Format FeeMonth (e.g., "2026-6" -> "June 2026")
    string formattedMonth = "N/A";
    if (DateTime.TryParseExact(feeMonth, "yyyy-M", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
    {
        formattedMonth = parsedDate.ToString("MMMM yyyy");
    }

    string receiptNo = $"{feeData.School.SchoolCode?.ToUpper() ?? "VOS"}/{feeMonth.Replace("-", "/")}/{feeData.Fee.StudentFeeId:D4}";

    return ApiResult<FeeReceiptResponse>.Ok(new FeeReceiptResponse
    {
        ReceiptNo = receiptNo,
        // 🚀 Pulled from Students table via join
        AcademicSession = feeData.Student.AcademicYear ?? "2025-26", 
        
        ReceiptDate = feeData.Fee.PaidOn?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow,
        SchoolName = feeData.School.SchoolName ?? "VidyaOS School",
        SchoolAddress = $"{feeData.School.AddressLine1}, {feeData.School.City}",
        StudentName = $"{feeData.Student.FirstName} {feeData.Student.LastName}",
        AdmissionNo = !string.IsNullOrEmpty(feeData.Student.AdmissionNo) ? feeData.Student.AdmissionNo : "N/A",
        ClassSection = $"{feeData.ClassName} - {feeData.SectionName}",
        
        // 🚀 Human readable month
        FeeMonth = formattedMonth, 
        Amount = feeData.Fee.Amount ?? 0,
        PaymentMode = feeData.Fee.PaymentMode ?? "Cash",
        RollNo = feeData.Student.RollNo?.ToString() ?? "N/A"
    });
}
        public async Task<ApiResult<List<StudentListDto>>> GetStudentsByClassSectionAsync(
    int schoolId, int classId, int sectionId, int? streamId = null)
        {
            var query = from s in _context.Students
                            // Use Left Join for Streams to ensure junior classes (null streams) still show up
                        join st in _context.Streams on s.StreamId equals st.StreamId into streamJoin
                        from st in streamJoin.DefaultIfEmpty()
                        where s.SchoolId == schoolId &&
                              s.ClassId == classId &&
                              s.SectionId == sectionId &&
                              s.IsActive == true
                        select new StudentListDto
                        {
                            StudentId = s.StudentId,
                            AdmissionNo = s.AdmissionNo ?? "N/A",
                            FullName = s.FirstName + " " + s.LastName,
                            RollNo = s.RollNo ?? 0,
                            StreamName = st != null ? st.StreamName : null
                        };

            if (streamId.HasValue && streamId.Value > 0)
            {
                query = query.Where(x => _context.Students.Any(s => s.StudentId == x.StudentId && s.StreamId == streamId));
            }

            // 🚀 .Distinct() ensures that if a student is mapped twice in a join, they only show once
            var students = await query.Distinct().OrderBy(s => s.RollNo).ToListAsync();

            return ApiResult<List<StudentListDto>>.Ok(students);
        }
        public async Task<ApiResult<TodayBirthdayResponse>> GetTodaysBirthdaysAsync(int schoolId)
        {
            // Birthdays should always use LOCAL date
            var today = DateOnly.FromDateTime(DateTime.Today);

            var students = await _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.IsActive == true &&
                    s.Dob.HasValue &&
                    s.Dob.Value.Month == today.Month &&
                    s.Dob.Value.Day == today.Day
                )
                .Select(s => new BirthdayPersonDto
                {
                    UserId = s.StudentId,
                    Name = s.FirstName + " " + s.LastName,
                    Role = "Student",
                    Dob = s.Dob!.Value
                })
                .ToListAsync();

            return ApiResult<TodayBirthdayResponse>.Ok(new TodayBirthdayResponse
            {
                Students = students
            });
        }

        public async Task<ApiResult<object>> GenerateRollNumbersAlphabeticallyAsync(
        int schoolId, int classId, int sectionId, int? streamId = null)
        {
            // 1. Build the initial query based on school, class, and section
            var query = _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.SectionId == sectionId &&
                    s.IsActive == true);

            // 2. Add the optional Stream filter if streamId is provided
            // This is useful for higher classes (11th & 12th)
            if (streamId.HasValue && streamId.Value > 0)
            {
                query = query.Where(s => s.StreamId == streamId.Value);
            }

            // 3. Order the students alphabetically and execute the query
            var students = await query
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();

            if (!students.Any())
                return ApiResult<object>.Fail("No students found for the selected criteria.");

            // 4. Assign sequential roll numbers
            int roll = 1;
            foreach (var student in students)
            {
                student.RollNo = roll++;
            }

            // 5. Save changes to the Azure SQL Database
            await _context.SaveChangesAsync();

            return ApiResult<object>.Ok(null,
                streamId.HasValue
                ? $"Roll numbers generated for stream {streamId.Value}."
                : "Roll numbers generated alphabetically for the entire section.");
        }

        public async Task<ApiResult<List<SubjectDropdownDto>>>
                GetSubjectsForClassSectionAsync(
                    int schoolId,
                    int classId,
                    int? sectionId)
        {
            var subjects = await _context.Subjects
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.IsActive == true
                )
                .Select(s => new SubjectDropdownDto
                {
                    SubjectId = s.SubjectId,
                    SubjectName = s.SubjectName
                })
                .Distinct()
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            return ApiResult<List<SubjectDropdownDto>>.Ok(subjects);
        }
        // Method 1: Get the list for the Directory
        public async Task<ApiResult<List<StudentSummaryDto>>> GetStudentsByClassSectionAsync(int schoolId, int classId, int sectionId,int streamId)
        {
            var data = await _context.Students
                .Where(s => s.SchoolId == schoolId &&
                            s.ClassId == classId &&
                            s.SectionId == sectionId &&
                            s.StreamId==streamId &&
                            s.IsActive == true)
                .Select(s => new StudentSummaryDto
                {
                    StudentId = s.StudentId,
                    FullName = s.FirstName + " " + s.LastName,
                    AdmissionNo = s.AdmissionNo,
                    ClassName = _context.Classes
                        .Where(c => c.SchoolId == s.SchoolId && c.ClassId == s.ClassId)
                        .Select(c => c.ClassName).FirstOrDefault() ?? "N/A"
                }).ToListAsync();

            return ApiResult<List<StudentSummaryDto>>.Ok(data);
        }

        // Method 2: Get the full detail for the Profile
        public async Task<ApiResult<StudentDetailsDto>> GetStudentDetailsAsync(int studentId)
        {
            var student = await _context.Students
                .AsNoTracking()
                .Where(s => s.StudentId == studentId)
                .Select(s => new StudentDetailsDto
                {
                    StudentId = s.StudentId,
                    // Concatenate names for the FullName property
                    FullName = (s.FirstName + " " + s.LastName).Trim(),
                    AdmissionNo = s.AdmissionNo,
                    RollNo = s.RollNo,
                    AcademicYear = s.AcademicYear,

                    // 🚀 Logic for Prefilling Selectors
                    ClassId = s.ClassId,
                    SectionId = s.SectionId,
                    StreamId = s.StreamId,
                    Gender = s.Gender,

                    // 📅 Date Conversion for Frontend Compatibility
                    Dob = s.Dob.HasValue
                        ? new DateTime(s.Dob.Value.Year, s.Dob.Value.Month, s.Dob.Value.Day)
                        : null,

                    // 🏢 Join School Name
                    SchoolName = _context.Schools
                        .Where(sch => sch.SchoolId == s.SchoolId)
                        .Select(sch => sch.SchoolName)
                        .FirstOrDefault() ?? "VidyaOS Academy",

                    // 🔑 Map Class Name based on ID
                    ClassName = _context.Classes
                        .Where(c => c.SchoolId == s.SchoolId && c.ClassId == s.ClassId)
                        .Select(c => c.ClassName).FirstOrDefault() ?? "N/A",

                    // 🔑 Map Section Name based on ID
                    SectionName = _context.Sections
                        .Where(sec => sec.SchoolId == s.SchoolId && sec.ClassId == s.ClassId && sec.SectionId == s.SectionId)
                        .Select(sec => sec.SectionName).FirstOrDefault() ?? "N/A",

                    ParentPhone = s.ParentPhone,
                    FatherName = s.FatherName,
                    MotherName = s.MotherName,
                    AddressLine1 = s.AddressLine1,
                    City = s.City,
                    State = s.State
                }).FirstOrDefaultAsync();

            if (student == null) return ApiResult<StudentDetailsDto>.Fail("Student not found.");

            return ApiResult<StudentDetailsDto>.Ok(student);
        }

        public async Task<ApiResult<bool>> UpdateStudentDetailsAsync(StudentDetailsDto dto)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentId);

            if (student == null) return ApiResult<bool>.Fail("Student record not found.");

            // 1. Name Handling (Handles Middle Names Correctly)
            var nameParts = dto.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length > 0)
            {
                student.FirstName = nameParts[0];
                student.LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
            }

            // 2. Academic Placement Update
            student.ClassId = dto.ClassId;
            student.SectionId = dto.SectionId;
            student.StreamId = dto.StreamId;

            // 3. Personal & Contact Info
            student.Gender = dto.Gender;
            if (dto.Dob.HasValue)
                student.Dob = DateOnly.FromDateTime(dto.Dob.Value);

            student.ParentPhone = dto.ParentPhone;
            student.FatherName = dto.FatherName;
            student.MotherName = dto.MotherName;
            student.AddressLine1 = dto.AddressLine1;
            student.City = dto.City;
            student.State = dto.State;

            try
            {
                // Note: Transactions are not manually started here because SaveChangesAsync 
                // implicitly uses one for single-entity updates.
                await _context.SaveChangesAsync();
                return ApiResult<bool>.Ok(true, "Student profile updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail($"Save failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        public async Task<ApiResult<List<LookUpDto>>> GetExamsOnlyAsync(int schoolId)
        {
            var exams = await _context.Exams.AsNoTracking()
                .Where(e => e.SchoolId == schoolId && e.IsActive == true)
                .Select(e => new LookUpDto { Id = e.ExamId, Name = e.ExamName }).ToListAsync();
            return ApiResult<List<LookUpDto>>.Ok(exams);
        }

        public async Task<ApiResult<List<LookUpDto>>> GetSubjectsByContextAsync(int schoolId, int classId, int? streamId)
        {
            var query = _context.Subjects.AsNoTracking()
                .Where(s => s.SchoolId == schoolId && s.ClassId == classId);

            if (streamId.HasValue && streamId > 0)
                query = query.Where(s => s.StreamId == streamId);

            var subjects = await query
                .Select(s => new LookUpDto { Id = s.SubjectId, Name = s.SubjectName }).ToListAsync();

            return ApiResult<List<LookUpDto>>.Ok(subjects);
        }

        public async Task<ApiResult<List<BulkMarksEntryDto>>> GetMarksEntryListAsync(int schoolId, int examId, int classId, int sectionId, int subjectId, int? streamId)
        {
            // 1. Fetch MaxMarks once for the whole class to save database resources
            var maxMarks = await _context.ExamSubjects
                .Where(es => es.ExamId == examId && es.SubjectId == subjectId)
                .Select(es => (int?)es.MaxMarks)
                .FirstOrDefaultAsync() ?? 100;

            // 2. Build student query with stream isolation
            var query = _context.Students.AsNoTracking()
                .Where(s => s.SchoolId == schoolId &&
                            s.ClassId == classId &&
                            s.SectionId == sectionId &&
                            s.IsActive == true);

            if (streamId.HasValue && streamId.Value > 0)
            {
                query = query.Where(s => s.StreamId == streamId.Value);
            }

            // 3. Use a Left Join for Marks to get everything in ONE database trip
            var data = await (from s in query
                              join m in _context.StudentMarks.Where(x => x.ExamId == examId && x.SubjectId == subjectId)
                              on s.StudentId equals m.StudentId into marksJoin
                              from m in marksJoin.DefaultIfEmpty()
                              select new BulkMarksEntryDto
                              {
                                  StudentId = s.StudentId,
                                  RollNo = s.RollNo,
                                  FullName = s.FirstName + " " + s.LastName,
                                  AdmissionNo = s.AdmissionNo,
                                  MarksObtained = (int?)m.MarksObtained, // Null if not entered yet
                                  MaxMarks = maxMarks
                              })
                              .OrderBy(x => x.RollNo)
                              .ToListAsync();

            return ApiResult<List<BulkMarksEntryDto>>.Ok(data);
        }
        public async Task<ApiResult<bool>> SaveBulkMarksAsync(BulkSaveRequest request)
        {
            // 1. Server-side Validation Loop
            foreach (var item in request.Marks)
            {
                if (item.MarksObtained.HasValue && item.MarksObtained > item.MaxMarks)
                {
                    return ApiResult<bool>.Fail($"Validation Error: {item.FullName}'s marks ({item.MarksObtained}) exceed max marks ({item.MaxMarks}).");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Optimized Fetching: Get all existing marks for this exam/subject in one trip
                var studentIds = request.Marks.Select(m => m.StudentId).ToList();
                var existingMarks = await _context.StudentMarks
                    .Where(m => m.ExamId == request.ExamId &&
                                m.SubjectId == request.SubjectId &&
                                studentIds.Contains(m.StudentId))
                    .ToListAsync();

                foreach (var item in request.Marks)
                {
                    var existing = existingMarks.FirstOrDefault(m => m.StudentId == item.StudentId);

                    if (existing != null)
                    {
                        // Update existing record
                        existing.MarksObtained = item.MarksObtained ?? 0;
                        existing.MaxMarks = item.MaxMarks; // Keep MaxMarks synced
                    }
                    else
                    {
                        // Add new record
                        _context.StudentMarks.Add(new StudentMark
                        {
                            SchoolId = request.SchoolId,
                            ExamId = request.ExamId,
                            ClassId = request.ClassId,
                            SubjectId = request.SubjectId,
                            StudentId = item.StudentId,
                            MarksObtained = item.MarksObtained ?? 0,
                            MaxMarks = item.MaxMarks,
                            // Optional: You can also store StreamId if your table has the column
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ApiResult<bool>.Ok(true, "Bulk marks updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log ex for debugging on Azure
                return ApiResult<bool>.Fail("Database error while saving. Please try again.");
            }
        }


        public async Task<ApiResult<StudentResultSummaryDto>> GetStudentResultSummaryAsync(int studentId, int examId)
        {
            try
            {
                // 1. Get Student and Class details using a Manual Join to avoid Navigation Property errors
                var studentData = await (from s in _context.Students
                                         join c in _context.Classes on s.ClassId equals c.ClassId
                                         where s.StudentId == studentId
                                         select new
                                         {
                                             s.FirstName,
                                             s.LastName,
                                             s.RollNo,
                                             s.AdmissionNo,
                                             c.ClassName
                                         }).FirstOrDefaultAsync();

                if (studentData == null) return ApiResult<StudentResultSummaryDto>.Fail("Student not found.");

                // 2. Get Exam Name simply by ID
                var examName = await _context.Exams
                    .Where(e => e.ExamId == examId)
                    .Select(e => e.ExamName)
                    .FirstOrDefaultAsync() ?? "Examination";

                // 3. Get Marks and Subject Names using Manual Join
                // This avoids looking for the "Subject" virtual property that triggers Error 207
                var marksList = await (from m in _context.StudentMarks
                                       join sub in _context.Subjects on m.SubjectId equals sub.SubjectId
                                       where m.StudentId == studentId && m.ExamId == examId
                                       select new SubjectMarkDto
                                       {
                                           SubjectName = sub.SubjectName,
                                           Obtained = m.MarksObtained, // Ensure this matches your DB column
                                           Max = m.MaxMarks          // Ensure this matches your DB column
                                       }).ToListAsync();

                if (!marksList.Any()) return ApiResult<StudentResultSummaryDto>.Fail("No marks found for this student in the selected exam.");

                // 4. Calculations
                int totalObtained = marksList.Sum(x => x.Obtained);
                int totalMax = marksList.Sum(x => x.Max);

                var summary = new StudentResultSummaryDto
                {
                    FullName = $"{studentData.FirstName} {studentData.LastName}",
                    RollNo = studentData.RollNo?.ToString() ?? "N/A",
                    AdmissionNo = studentData.AdmissionNo ?? "N/A",
                    ExamName = examName,
                    ClassName = studentData.ClassName,
                    Marks = marksList,
                    TotalObtained = totalObtained,
                    TotalMax = totalMax,
                    Percentage = totalMax > 0 ? Math.Round((double)totalObtained / totalMax * 100, 2) : 0
                };

                return ApiResult<StudentResultSummaryDto>.Ok(summary);
            }
            catch (Exception ex)
            {
                // This will now catch the specific error if a column name is still misspelled
                return ApiResult<StudentResultSummaryDto>.Fail("Internal Error: " + ex.Message);
            }
        }


        public async Task<ApiResult<List<UserLeaveHistoryDto>>> GetUserLeaveHistoryAsync(int schoolId, int userId)
        {
            var history = await _context.Leaves
                .Where(l => l.SchoolId == schoolId && l.UserId == userId)
                .OrderByDescending(l => l.AppliedOn) // Latest first
                .Select(l => new UserLeaveHistoryDto
                {
                    LeaveId = l.LeaveId,
                    FromDate = l.FromDate ?? DateOnly.FromDateTime(DateTime.Now),
                    ToDate = l.ToDate ?? DateOnly.FromDateTime(DateTime.Now),
                    Reason = l.Reason ?? "",
                    Status = l.Status ?? "Pending",
                    AppliedOn = l.AppliedOn ?? DateOnly.FromDateTime(DateTime.Now)
                })
                .ToListAsync();

            return ApiResult<List<UserLeaveHistoryDto>>.Ok(history);
        }

        public async Task<ApiResult<StudentAttendanceResponse>> GetStudentMonthlyAttendanceAsync(int userId, int month, int year)
        {
            // 1. Fetch records for the specific user and month/year
            // EF Core translates .Month and .Year of DateOnly directly to SQL
            var records = await _context.Attendances
                .Where(a => a.UserId == userId &&
                            a.AttendanceDate.HasValue &&
                            a.AttendanceDate.Value.Month == month &&
                            a.AttendanceDate.Value.Year == year)
                .OrderBy(a => a.AttendanceDate)
                .Select(a => new StudentAttendanceRecordDto
                {
                    Date = a.AttendanceDate.Value,
                    Status = a.Status ?? "Absent"
                })
                .ToListAsync();

            // 2. Calculate Summary Statistics
            int total = records.Count;
            int present = records.Count(r => r.Status == "Present");
            int absent = records.Count(r => r.Status == "Absent");

            decimal percentage = total > 0 ? (decimal)present / total * 100 : 0;

            var response = new StudentAttendanceResponse
            {
                TotalDays = total,
                PresentCount = present,
                AbsentCount = absent,
                AttendancePercentage = Math.Round(percentage, 2),
                Records = records
            };

            return ApiResult<StudentAttendanceResponse>.Ok(response);
        }

        public async Task<ApiResult<List<FeeHistoryDto>>> GetStudentFeeStatusAsync(int schoolId, int studentId)
        {
            var school = await _context.Schools.FirstOrDefaultAsync(s => s.SchoolId == schoolId);
            string schoolCode = school?.SchoolCode ?? "SCH";

            var fees = await _context.StudentFees
                .Where(f => f.SchoolId == schoolId && f.StudentId == studentId)
                .OrderBy(f => f.StudentFeeId) // Order by month sequence
                .Select(f => new FeeHistoryDto
                {
                    StudentFeeId = f.StudentFeeId,
                    FeeMonth = f.FeeMonth ?? "",
                    Amount = f.Amount ?? 0,
                    Status = f.Status ?? "Pending", // Added Status to DTO
                    PaidOn = f.PaidOn,
                    PaymentMode = f.PaymentMode ?? "N/A",
                    // Receipt logic only applies if PaidOn has a value
                    ReceiptNo = f.Status == "Paid" && f.PaidOn.HasValue
                                ? $"REC/{schoolCode}/{f.PaidOn.Value:yyyyMMdd}/{f.StudentFeeId}"
                                : ""
                })
                .ToListAsync();

            return ApiResult<List<FeeHistoryDto>>.Ok(fees);
        }

        public async Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync(int schoolId, int userId, string role)
        {
            try
            {
                var summary = new DashboardSummaryDto();

                // 1. Common School Name
                summary.SchoolName = await _context.Schools
                    .Where(s => s.SchoolId == schoolId)
                    .Select(s => s.SchoolName)
                    .FirstOrDefaultAsync() ?? "VidyaOS School";

                if (role == "SchoolAdmin")
                {
                    summary.TotalStudents = await _context.Students.CountAsync(s => s.SchoolId == schoolId && s.IsActive==true);
                    summary.TotalTeachers = await _context.Teachers.CountAsync(t => t.SchoolId == schoolId && t.IsActive==true);
                    // ... Add subscription logic here ...
                }
                else if (role == "Student")
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (student != null)
                    {
                        summary.StudentId = student.StudentId;
                        summary.FullName = student.FirstName + " "+student.LastName;
                        summary.AdmissionNo = student.AdmissionNo;
                        summary.RollNo = student.RollNo;

                        // 🚀 Calculate Attendance using your Attendance model
                        // We count records where the Status is 'Present' vs Total records for this User
                        var attendanceQuery = _context.Attendances.Where(a => a.UserId == userId);

                        int totalDays = await attendanceQuery.CountAsync();
                        int presentDays = await attendanceQuery.CountAsync(a => a.Status == "Present");

                        summary.AttendancePercentage = totalDays > 0
                            ? Math.Round((double)presentDays / totalDays * 100, 1)
                            : 0;
                    }
                }
                else if (role == "Teacher")
                {
                    var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
                    if (teacher != null)
                    {
                        summary.FullName = teacher.FullName;
                        
                    }
                }

                return ApiResult<DashboardSummaryDto>.Ok(summary);
            }
            catch (Exception ex)
            {
                return ApiResult<DashboardSummaryDto>.Fail("Error: " + ex.Message);
            }
        }

        public async Task<ApiResult<bool>> AddMasterSubjectAsync(MasterSubjectDto dto)
        {
            var exists = await _context.MasterSubjects.AnyAsync(m =>
                m.SchoolId == dto.SchoolId &&
                m.SubjectName.ToLower() == dto.SubjectName.ToLower());

            if (exists) return ApiResult<bool>.Fail("Subject already exists in Master list.");

            var master = new MasterSubject
            {
                SchoolId = dto.SchoolId,
                SubjectName = dto.SubjectName,
                StreamId = dto.StreamId,
                IsActive = dto.IsActive
            };

            _context.MasterSubjects.Add(master);
            await _context.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "Master subject added.");
        }

        public async Task<ApiResult<List<MasterSubjectResponseDto>>> GetAllMasterSubjectsAsync(int schoolId)
        {
            // Use .MasterSubjects (plural) to resolve your CS1061 error
            var subjects = await _context.MasterSubjects
                .Where(m => m.SchoolId == schoolId)
                .OrderBy(m => m.SubjectName)
                .Select(m => new MasterSubjectResponseDto
                {
                    MasterSubjectId = m.MasterSubjectId,
                    SubjectName = m.SubjectName,
                    StreamId = m.StreamId,
                    // Assuming you have a Streams table in your context
                    StreamName = _context.Streams
                        .Where(s => s.StreamId == m.StreamId)
                        .Select(s => s.StreamName)
                        .FirstOrDefault(),
                    IsActive = m.IsActive ?? false
                })
                .ToListAsync();

            return ApiResult<List<MasterSubjectResponseDto>>.Ok(subjects, "Master subjects retrieved successfully.");
        }
        public async Task<ApiResult<bool>> UpdateMasterSubjectAsync(MasterSubjectDto dto)
        {
            var master = await _context.MasterSubjects.FirstOrDefaultAsync(m =>
                m.MasterSubjectId == dto.MasterSubjectId && m.SchoolId == dto.SchoolId);

            if (master == null) return ApiResult<bool>.Fail("Master subject not found.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Sync Logic: If name changed, update all existing assignments in Subjects table
                if (master.SubjectName != dto.SubjectName)
                {
                    var assignments = await _context.Subjects
                        .Where(s => s.SchoolId == dto.SchoolId && s.SubjectName == master.SubjectName)
                        .ToListAsync();

                    foreach (var assignment in assignments)
                    {
                        assignment.SubjectName = dto.SubjectName;
                    }
                }

                master.SubjectName = dto.SubjectName;
                master.StreamId = dto.StreamId;
                master.IsActive = dto.IsActive;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ApiResult<bool>.Ok(true, "Master list and assigned subjects synced.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResult<bool>.Fail($"Sync error: {ex.Message}");
            }
        }

        // 3. Assign Subject to Class
        public async Task<ApiResult<bool>> AssignSubjectToClassAsync(AssignSubjectDto dto)
        {
            // 🚀 FIXED: Check existence including StreamId to allow the same subject in different streams
            var exists = await _context.Subjects.AnyAsync(s =>
                s.SchoolId == dto.SchoolId &&
                s.ClassId == dto.ClassId &&
                s.SubjectName == dto.SubjectName &&
                s.StreamId == dto.StreamId); // Added this to differentiate PCM/PCB/Arts/etc.

            if (exists)
            {
                return ApiResult<bool>.Fail("This subject is already assigned to this specific class and stream.");
            }

            var subject = new Subject
            {
                SchoolId = dto.SchoolId,
                ClassId = dto.ClassId,
                // Ensure that for junior classes, we pass null if streamId is 0 from the frontend
                StreamId = dto.StreamId > 0 ? dto.StreamId : null,
                SubjectName = dto.SubjectName,
                IsActive = true
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return ApiResult<bool>.Ok(true, "Subject assigned successfully.");
        }

        public async Task<ApiResult<List<AssignedSubjectResponseDto>>> GetAssignedSubjectsAsync(int schoolId, int classId, int? streamId)
        {
            // Start with core school and class filters
            var query = _context.Subjects.AsNoTracking()
                .Where(s => s.SchoolId == schoolId && s.ClassId == classId);

            // 🚀 Apply Stream isolation
            // If streamId is provided (Class 11/12), filter by it.
            // If not provided (Junior classes), look for subjects where StreamId is null.
            if (streamId.HasValue && streamId.Value > 0)
            {
                query = query.Where(s => s.StreamId == streamId.Value);
            }
            else if (classId < 11)
            {
                query = query.Where(s => s.StreamId == null);
            }

            var data = await query
                .Select(s => new AssignedSubjectResponseDto
                {
                    SubjectId = s.SubjectId,
                    SubjectName = s.SubjectName,
                    ClassId = s.ClassId,
                    StreamId = s.StreamId // Added to DTO for frontend tracking
                })
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            return ApiResult<List<AssignedSubjectResponseDto>>.Ok(data);
        }

        // 4. Delete Assigned Subject (Class level only)
        public async Task<ApiResult<bool>> DeleteAssignedSubjectAsync(int subjectId, int schoolId)
        {
            var item = await _context.Subjects
                .FirstOrDefaultAsync(s => s.SubjectId == subjectId && s.SchoolId == schoolId);

            if (item == null) return ApiResult<bool>.Fail("Assigned subject not found.");

            // 🚀 Soft Delete: Keeps data intact but hides it from UI
            item.IsActive = false;

            await _context.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "Subject unassigned and deactivated.");
        }

        public async Task<ApiResult<bool>> UpdateTimetableBulkAsync(TimetableBulkRequest req)
        {
            // Use a transaction to ensure database integrity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Clear existing schedule for this specific class and section
                var existing = _context.ClassTimetables.Where(t =>
                    t.SchoolId == req.SchoolId &&
                    t.ClassId == req.ClassId &&
                    t.SectionId == req.SectionId);

                _context.ClassTimetables.RemoveRange(existing);

                // 2. Map and Add new entries
                foreach (var item in req.Entries)
                {
                    var newEntry = new ClassTimetable
                    {
                        SchoolId = req.SchoolId,
                        ClassId = req.ClassId,
                        SectionId = req.SectionId,
                        SubjectId = item.SubjectId,
                        DayOfWeek = MapDayToInt(item.DayOfWeek),
                        PeriodNo = item.PeriodNo,
                        StartTime = TimeOnly.Parse(item.StartTime),
                        EndTime = TimeOnly.Parse(item.EndTime),
                        EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
                        AcademicYear = req.AcademicYear ?? "2025-26",
                        // EXPLICIT FIX: Ensure IsActive is set to true before saving 
                        // to avoid the NULL constraint error seen in Azure logs.
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ClassTimetables.Add(newEntry);
                }

                // 3. Save changes and commit transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResult<bool>.Ok(true, "Timetable synchronized successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Logging the most detailed error possible
                var message = ex.InnerException?.Message ?? ex.Message;
                return ApiResult<bool>.Fail($"Error saving timetable: {message}");
            }
        }

        public async Task<ApiResult<List<object>>> GetTimetableAsync(int schoolId, int classId, int sectionId, int? streamId)
        {
            try
            {
                var data = await (from ct in _context.ClassTimetables
                                  join s in _context.Subjects on ct.SubjectId equals s.SubjectId
                                  where ct.SchoolId == schoolId
                                     && ct.ClassId == classId
                                     && ct.SectionId == sectionId
                                  select new
                                  {
                                      DayOfWeek = ct.DayOfWeek == 1 ? "Monday" :
                                                  ct.DayOfWeek == 2 ? "Tuesday" :
                                                  ct.DayOfWeek == 3 ? "Wednesday" :
                                                  ct.DayOfWeek == 4 ? "Thursday" :
                                                  ct.DayOfWeek == 5 ? "Friday" :
                                                  ct.DayOfWeek == 6 ? "Saturday" : "Sunday",
                                      ct.PeriodNo,
                                      ct.SubjectId,
                                      SubjectName = s.SubjectName,
                                      StartTime = ct.StartTime.ToString("HH:mm"),
                                      EndTime = ct.EndTime.ToString("HH:mm")
                                  }).ToListAsync();

                return ApiResult<List<object>>.Ok(data.Cast<object>().ToList());
            }
            catch (Exception ex)
            {
                return ApiResult<List<object>>.Fail($"Failed to fetch timetable: {ex.Message}");
            }
        }

        private int MapDayToInt(string day) => day switch
        {
            "Monday" => 1,
            "Tuesday" => 2,
            "Wednesday" => 3,
            "Thursday" => 4,
            "Friday" => 5,
            "Saturday" => 6,
            "Sunday" => 7,
            _ => 1
        };

        public async Task<ApiResult<List<StreamDto>>> GetStreamsByClassAsync(int schoolId, int classId)
        {
            try
            {
                // 🚀 Only Classes 11 and 12 should have streams in VidyaOS
                if (classId != 11 && classId != 12)
                    return ApiResult<List<StreamDto>>.Ok(new List<StreamDto>(), "No streams required for this class.");

                var streams = await _context.Streams
                    .AsNoTracking()
                    .Where(s => s.SchoolId == schoolId && s.ClassId == classId && s.IsActive == true)
                    .Select(s => new StreamDto
                    {
                        StreamId = s.StreamId,
                        StreamName = s.StreamName
                    })
                    .ToListAsync();

                return ApiResult<List<StreamDto>>.Ok(streams);
            }
            catch (Exception ex)
            {
                return ApiResult<List<StreamDto>>.Fail($"Failed to fetch streams: {ex.Message}");
            }
        }

        public async Task<ApiResult<SchoolProfileDto>> GetSchoolProfileAsync(int schoolId)
        {
            var school = await _context.Schools
                .AsNoTracking()
                .Where(s => s.SchoolId == schoolId)
                .Select(s => new SchoolProfileDto
                {
                    SchoolId = s.SchoolId,
                    SchoolName = s.SchoolName,
                    SchoolCode = s.SchoolCode,
                    RegistrationNumber = s.RegistrationNumber,
                    YearOfFoundation = s.YearOfFoundation,
                    BoardType = s.BoardType,
                    AffiliationNumber = s.AffiliationNumber,
                    Email = s.Email,
                    Phone = s.Phone,
                    AddressLine1 = s.AddressLine1,
                    City = s.City,
                    State = s.State,
                    Pincode = s.Pincode
                })
                .FirstOrDefaultAsync();

            if (school == null) return ApiResult<SchoolProfileDto>.Fail("School not found.");

            return ApiResult<SchoolProfileDto>.Ok(school);
        }

        public async Task<ApiResult<bool>> UpdateSchoolProfileAsync(SchoolProfileDto dto)
        {
            var school = await _context.Schools.FirstOrDefaultAsync(s => s.SchoolId == dto.SchoolId);

            if (school == null) return ApiResult<bool>.Fail("School not found.");

            // Map DTO values back to the Model
            school.SchoolName = dto.SchoolName;
            school.RegistrationNumber = dto.RegistrationNumber;
            school.YearOfFoundation = dto.YearOfFoundation;
            school.BoardType = dto.BoardType;
            school.AffiliationNumber = dto.AffiliationNumber;
            school.Email = dto.Email;
            school.Phone = dto.Phone;
            school.AddressLine1 = dto.AddressLine1;
            school.City = dto.City;
            school.State = dto.State;
            school.Pincode = dto.Pincode;

            await _context.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "Profile updated successfully.");
        }

    }
}






