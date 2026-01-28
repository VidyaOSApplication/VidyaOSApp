using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.DTOs.VidyaOS.Models.DTOs;
using VidyaOSDAL.Models;
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

                // Generate School Code if not provided
                var schoolCode = string.IsNullOrWhiteSpace(req.SchoolCode)
                    ? req.SchoolName.Substring(0, Math.Min(3, req.SchoolName.Length)).ToUpper() + new Random().Next(100, 999)
                    : req.SchoolCode.Trim().ToUpper();

                if (await _context.Schools.AnyAsync(s => s.SchoolCode == schoolCode))
                    return ApiResult<RegisterSchoolResponse>.Fail("School code already exists.");

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
                            new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "PCM" },
                            new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "PCB" },
                            new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "Commerce" },
                            new VidyaOSDAL.Models.Stream { SchoolId = school.SchoolId, ClassId = cls.Id, StreamName = "Arts" }
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
                // Log the actual exception here
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
        public async Task<ApiResult<LeaveResponse>> ApplyLeaveAsync(
    ApplyLeaveRequest req)
        {
            if (req == null)
                return ApiResult<LeaveResponse>.Fail("Request is required.");

            if (req.FromDate.Date > req.ToDate.Date)
                return ApiResult<LeaveResponse>.Fail(
                    "From date cannot be greater than To date."
                );

            var fromDate = DateOnly.FromDateTime(req.FromDate);
            var toDate = DateOnly.FromDateTime(req.ToDate);

            bool isUpdated = false;

            // 🔍 Check overlapping leave
            var existingLeave = await _context.Leaves
                .FirstOrDefaultAsync(l =>
                    l.SchoolId == req.SchoolId &&
                    l.UserId == req.StudentId &&
                    l.FromDate <= toDate &&
                    l.ToDate >= fromDate
                );

            LeaveRequest targetLeave;

            if (existingLeave != null)
            {
                // 🔁 UPDATE
                existingLeave.FromDate = fromDate;
                existingLeave.ToDate = toDate;
                existingLeave.Reason = req.Reason;
                existingLeave.Status = "Pending";
                existingLeave.AppliedOn = DateOnly.FromDateTime(DateTime.UtcNow);

                targetLeave = existingLeave;
                isUpdated = true;
            }
            else
            {
                // ➕ CREATE
                targetLeave = new LeaveRequest
                {
                    SchoolId = req.SchoolId,
                    UserId = req.StudentId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Reason = req.Reason,
                    Status = "Pending",
                    AppliedOn = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                _context.Leaves.Add(targetLeave);
            }

            await _context.SaveChangesAsync();

            return ApiResult<LeaveResponse>.Ok(
                new LeaveResponse
                {
                    LeaveId = targetLeave.LeaveId,
                    Status = targetLeave.Status!,
                    AppliedAt = DateOnly.FromDateTime(DateTime.UtcNow)
                },
                isUpdated
                    ? "Leave updated successfully."
                    : "Leave applied successfully."
            );
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

            // 1️⃣ Fetch all active students and existing fee records for this month in one go
            var students = await _context.Students
                .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                .ToListAsync();

            var existingFeeStudentIds = await _context.StudentFees
                    .Where(f => f.FeeMonth == feeMonth)
                    .Select(f => (int)f.StudentId) // Explicitly cast to int
                    .ToListAsync();

            // 2️⃣ Fetch all active fee structures for the school
            var feeStructures = await _context.FeeStructures
                .Where(f => f.SchoolId == schoolId && f.IsActive == true)
                .ToListAsync();

            var newFees = new List<StudentFee>();

            foreach (var student in students)
            {
                // Skip if fee already exists (Checking in-memory list is much faster than DB)
                if (existingFeeStudentIds.Contains(student.StudentId))
                    continue;

                // 3️⃣ Find the correct fee structure using the "Senior Class" fix
                bool isSenior = (student.ClassId == 11 || student.ClassId == 12);

                var structure = feeStructures.FirstOrDefault(f =>
                    f.ClassId == student.ClassId &&
                    (!isSenior || f.StreamId== student.StreamId)
                );

                if (structure != null)
                {
                    newFees.Add(new StudentFee
                    {
                        StudentId = student.StudentId,
                        FeeMonth = feeMonth,
                        Amount = structure.MonthlyAmount,
                        Status = "Pending",
                         // Good practice for tracking
                    });
                }
            }

            // 4️⃣ Bulk Add and Save
            if (newFees.Any())
            {
                _context.StudentFees.AddRange(newFees);
                await _context.SaveChangesAsync();
            }

            return ApiResult<string>.Ok($"{newFees.Count} fees generated successfully.");
        }


        public async Task<ApiResult<List<PendingFeeResponse>>> GetPendingFeesAsync(
    int schoolId)
        {
            var data = await (
                from sf in _context.StudentFees
                join s in _context.Students on sf.StudentId equals s.StudentId
                join c in _context.Classes on s.ClassId equals c.ClassId
                join sec in _context.Sections on s.SectionId equals sec.SectionId
                where s.SchoolId == schoolId && sf.Status == "Pending"
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
            ).ToListAsync();

            return ApiResult<List<PendingFeeResponse>>.Ok(data);
        }
        public async Task<ApiResult<CollectFeesResponse>> CollectFeesAsync(
    CollectFeesRequest req)
        {
            if (req.StudentId <= 0 || req.SchoolId <= 0)
                return ApiResult<CollectFeesResponse>
                    .Fail("Invalid student or school.");

            if (req.FeeMonths == null || !req.FeeMonths.Any())
                return ApiResult<CollectFeesResponse>
                    .Fail("Select at least one fee month.");

            var fees = await _context.StudentFees
                .Where(f =>
                    f.StudentId == req.StudentId &&
                    req.FeeMonths.Contains(f.FeeMonth!) &&
                    f.Status == "Pending")
                .ToListAsync();

            if (!fees.Any())
                return ApiResult<CollectFeesResponse>
                    .Fail("No pending fees found.");

            decimal totalAmount = fees.Sum(f => f.Amount ?? 0);

            foreach (var fee in fees)
            {
                fee.Status = "Paid";
                fee.PaymentMode = req.PaymentMode;
                fee.PaidOn = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            await _context.SaveChangesAsync();

            var school = await _context.Schools
                .FirstAsync(s => s.SchoolId == req.SchoolId);

            string receiptNo =
                $"{school.SchoolCode}/{DateTime.UtcNow:yyyy}/{fees.First().StudentFeeId}";

            return ApiResult<CollectFeesResponse>.Ok(
                new CollectFeesResponse
                {
                    ReceiptNo = receiptNo,
                    StudentId = req.StudentId,
                    PaidMonths = fees.Select(f => f.FeeMonth!).ToList(),
                    TotalAmount = totalAmount,
                    PaidOn = DateTime.UtcNow
                },
                "Fee collected successfully."
            );
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
        public async Task<ApiResult<List<StudentFeeHistoryResponse>>>
    GetStudentFeeHistoryAsync(int studentId)
        {
            if (studentId <= 0)
                return ApiResult<List<StudentFeeHistoryResponse>>
                    .Fail("Invalid student id.");

            var fees = await _context.StudentFees
                .Where(f => f.StudentId == studentId)
                .OrderByDescending(f => f.FeeMonth)
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

            return ApiResult<List<StudentFeeHistoryResponse>>.Ok(fees);
        }


        public async Task<ApiResult<FeeReceiptResponse>> GenerateFeeReceiptAsync(
    int studentId, string feeMonth)
        {
            var fee = await _context.StudentFees
                .FirstOrDefaultAsync(f =>
                    f.StudentId == studentId &&
                    f.FeeMonth == feeMonth &&
                    f.Status == "Paid");

            if (fee == null)
                return ApiResult<FeeReceiptResponse>
                    .Fail("Paid fee record not found.");

            var student = await _context.Students
                .FirstAsync(s => s.StudentId == studentId);

            var school = await _context.Schools
                .FirstAsync(s => s.SchoolId == student.SchoolId);

            var className = await _context.Classes
                .Where(c => c.ClassId == student.ClassId)
                .Select(c => c.ClassName)
                .FirstAsync();

            var sectionName = await _context.Sections
                .Where(s => s.SectionId == student.SectionId)
                .Select(s => s.SectionName)
                .FirstAsync();

            string receiptNo =
                $"{school.SchoolCode}/{feeMonth.Replace("-", "/")}/{fee.StudentFeeId}";

            return ApiResult<FeeReceiptResponse>.Ok(
                new FeeReceiptResponse
                {
                    ReceiptNo = receiptNo,
                    ReceiptDate = fee.PaidOn?.ToDateTime(TimeOnly.MinValue)
                                  ?? DateTime.UtcNow,

                    SchoolName = school.SchoolName!,
                    SchoolAddress = $"{school.AddressLine1}, {school.City}",

                    StudentName = $"{student.FirstName} {student.LastName}",
                    AdmissionNo = student.AdmissionNo!,
                    ClassSection = $"{className}-{sectionName}",

                    FeeMonth = feeMonth,
                    Amount = fee.Amount ?? 0,
                    PaymentMode = fee.PaymentMode ?? "Cash"
                }
            );
        }
        public async Task<ApiResult<List<StudentListDto>>> GetStudentsByClassSectionAsync(
            int schoolId,
            int classId,
            int sectionId,
            int? streamId = null)
        {
            var students = await (
                from s in _context.Students
                    // Left Join with Streams
                join st in _context.Streams on s.StreamId equals st.StreamId into streamJoin
                from st in streamJoin.DefaultIfEmpty()

                where s.SchoolId == schoolId &&
                      s.ClassId == classId &&
                      s.SectionId == sectionId &&
                      s.IsActive == true &&
                      (!streamId.HasValue || s.StreamId == streamId)

                orderby s.RollNo
                select new StudentListDto
                {
                    StudentId = s.StudentId,
                    AdmissionNo = s.AdmissionNo!,
                    FullName = s.FirstName + " " + s.LastName,
                    RollNo = s.RollNo ?? 0,
                    // Only provide stream name if it exists (for 11 & 12)
                    StreamName = st != null ? st.StreamName : null
                }
            ).ToListAsync();

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
             int schoolId, int classId, int sectionId)
        {
            var students = await _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.SectionId == sectionId &&
                    s.IsActive == true)
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();

            if (!students.Any())
                return ApiResult<object>.Fail("No students found.");

            int roll = 1;
            foreach (var student in students)
            {
                student.RollNo = roll++;
            }

            await _context.SaveChangesAsync();

            return ApiResult<object>.Ok(null,
                "Roll numbers generated alphabetically.");
        }
    //    public async Task<ApiResult<string>> CreateClassTimetableAsync(
    //CreateTimetableRequest req)
    //    {
    //        if (req == null)
    //            return ApiResult<string>.Fail("Request is required.");

    //        // ⏱ Parse time
    //        if (!TimeOnly.TryParse(req.StartTime, out var startTime))
    //            return ApiResult<string>.Fail("Invalid start time.");

    //        if (!TimeOnly.TryParse(req.EndTime, out var endTime))
    //            return ApiResult<string>.Fail("Invalid end time.");

    //        if (startTime >= endTime)
    //            return ApiResult<string>.Fail(
    //                "Start time must be before end time."
    //            );

    //        // 📅 Effective date validation
    //        if (req.EffectiveTo.HasValue &&
    //            req.EffectiveFrom > req.EffectiveTo.Value)
    //        {
    //            return ApiResult<string>.Fail(
    //                "Effective From date cannot be after Effective To date."
    //            );
    //        }

    //        // ❌ Period number duplicate check
    //        bool periodExists = await _context.ClassTimetables.AnyAsync(t =>
    //            t.SchoolId == req.SchoolId &&
    //            t.ClassId == req.ClassId &&
    //            t.SectionId == req.SectionId &&
    //            t.DayOfWeek == req.DayOfWeek &&
    //            t.PeriodNo == req.PeriodNo &&
    //            t.AcademicYear == req.AcademicYear &&
    //            t.IsActive
    //        );

    //        if (periodExists)
    //            return ApiResult<string>.Fail(
    //                $"Period {req.PeriodNo} already exists for this day."
    //            );

    //        // ❌ Time overlap check (correct + academic year safe)
    //        bool overlap = await _context.ClassTimetables.AnyAsync(t =>
    //            t.SchoolId == req.SchoolId &&
    //            t.ClassId == req.ClassId &&
    //            t.SectionId == req.SectionId &&
    //            t.DayOfWeek == req.DayOfWeek &&
    //            t.AcademicYear == req.AcademicYear &&
    //            t.IsActive &&
    //            startTime < t.EndTime &&
    //            endTime > t.StartTime
    //        );

    //        if (overlap)
    //            return ApiResult<string>.Fail(
    //                "Timetable period overlaps with an existing period."
    //            );

    //        // ✅ Insert timetable
    //        var timetable = new ClassTimetable
    //        {
    //            SchoolId = req.SchoolId,
    //            ClassId = req.ClassId,
    //            SectionId = req.SectionId,
    //            SubjectId = req.SubjectId,

    //            DayOfWeek = req.DayOfWeek,
    //            PeriodNo = req.PeriodNo,

    //            StartTime = startTime,
    //            EndTime = endTime,

    //            EffectiveFrom = req.EffectiveFrom,
    //            EffectiveTo = req.EffectiveTo,
    //            AcademicYear = req.AcademicYear,

    //            IsActive = true,
    //            CreatedAt = DateTime.UtcNow
    //        };

    //        _context.ClassTimetables.Add(timetable);
    //        await _context.SaveChangesAsync();

    //        return ApiResult<string>.Ok(
    //            "Class timetable created successfully."
    //        );
    //    }



    //    public async Task<ApiResult<List<TimetableResponse>>> GetClassTimetableAsync(
    //int schoolId,
    //int classId,
    //int? sectionId,
    //string academicYear)
    //    {
    //        var timetable = await (
    //            from t in _context.ClassTimetables
    //            join s in _context.Subjects
    //                on new { t.SubjectId, t.ClassId }
    //                equals new { s.SubjectId, s.ClassId }

    //            where
    //                t.SchoolId == schoolId &&
    //                t.ClassId == classId &&
    //                t.AcademicYear == academicYear &&
    //                t.IsActive == true &&
    //                s.IsActive==true &&
    //                s.SchoolId == schoolId &&
    //                (
    //                    sectionId.HasValue
    //                        ? t.SectionId == sectionId.Value
    //                        : t.SectionId == null
    //                )

    //            orderby t.DayOfWeek, t.PeriodNo

    //            select new TimetableResponse
    //            {
    //                TimetableId = t.TimetableId,
    //                DayOfWeek = t.DayOfWeek,
    //                PeriodNo = t.PeriodNo,
    //                StartTime = t.StartTime.ToString("hh:mm tt"),
    //                EndTime = t.EndTime.ToString("hh:mm tt"),
    //                SubjectName = s.SubjectName
    //            }
    //        ).ToListAsync();

    //        return ApiResult<List<TimetableResponse>>.Ok(timetable);
    //    }



    //    public async Task<ApiResult<string>> AssignSubjectsToClassAsync(
    //AssignClassSubjectsRequest req)
    //    {
    //        if (req.SubjectIds == null || !req.SubjectIds.Any())
    //            return ApiResult<string>.Fail("No subjects selected");

    //        // 1️⃣ Fetch master subjects
    //        var masterSubjects = await _context.MasterSubjects
    //            .Where(ms =>
    //                ms.SchoolId == req.SchoolId &&
    //                req.SubjectIds.Contains(ms.MasterSubjectId))
    //            .ToListAsync();

    //        if (!masterSubjects.Any())
    //            return ApiResult<string>.Fail("Master subjects not found");

    //        // 2️⃣ Fetch existing subjects (IMPORTANT: NULL SAFE)
    //        var existingSubjects = await _context.Subjects
    //            .Where(s =>
    //                s.SchoolId == req.SchoolId &&
    //                s.ClassId == req.ClassId &&
    //                (
    //                    (req.StreamId == null && s.StreamId == null) ||
    //                    (req.StreamId != null && s.StreamId == req.StreamId)
    //                )
    //            )
    //            .ToListAsync();

    //        var existingNames = existingSubjects
    //            .Select(s => s.SubjectName)
    //            .ToHashSet();

    //        // 3️⃣ Insert ONLY missing subjects (NO DELETE)
    //        var subjectsToInsert = masterSubjects
    //            .Where(ms => !existingNames.Contains(ms.SubjectName))
    //            .Select(ms => new Subject
    //            {
    //                SchoolId = req.SchoolId,
    //                ClassId = req.ClassId,
    //                StreamId = req.StreamId,   // null for 1–10
    //                SubjectName = ms.SubjectName!,
    //                IsActive = true
    //            })
    //            .ToList();

    //        if (subjectsToInsert.Any())
    //        {
    //            _context.Subjects.AddRange(subjectsToInsert);
    //            await _context.SaveChangesAsync();
    //        }

    //        return ApiResult<string>.Ok("Subjects assigned successfully");
    //    }


    //    public async Task<ApiResult<List<SubjectAssignResponse>>>
    //     GetSubjectsForClassAsync(int schoolId, int classId, int? streamId)
    //    {
    //        var masterSubjects = await _context.MasterSubjects
    //            .Where(m =>
    //                m.SchoolId == schoolId &&
    //                m.IsActive==true &&
    //                (classId <= 10 || m.StreamId == streamId || m.StreamId == null))
    //            .ToListAsync();

    //        var assigned = await _context.Subjects
    //            .Where(s =>
    //                s.SchoolId == schoolId &&
    //                s.ClassId == classId &&
    //                (classId <= 10 || s.StreamId == streamId))
    //            .ToListAsync();

    //        var result = masterSubjects.Select(m => new SubjectAssignResponse
    //        {
    //            SubjectId = m.MasterSubjectId,
    //            SubjectName = m.SubjectName,
    //            Assigned = assigned.Any(a => a.SubjectName == m.SubjectName)
    //        }).ToList();

    //        return ApiResult<List<SubjectAssignResponse>>.Ok(result);
    //    }
    //    public async Task<ApiResult<List<MasterSubjectDto>>> GetMasterSubjectsAsync(int schoolId)
    //    {
    //        var subjects = await _context.MasterSubjects
    //            .Where(ms => ms.SchoolId == schoolId && ms.IsActive == true)
    //            .OrderBy(ms => ms.SubjectName)
    //            .Select(ms => new MasterSubjectDto
    //            {
    //                MasterSubjectId = ms.MasterSubjectId,
    //                SubjectName = ms.SubjectName
    //            })
    //            .ToListAsync();

    //        return ApiResult<List<MasterSubjectDto>>.Ok(subjects);
    //    }
    //    public async Task<ApiResult<bool>> DeleteMasterSubjectAsync(int id)
    //    {
    //        var subject = await _context.MasterSubjects.FirstOrDefaultAsync(s => s.MasterSubjectId == id);

    //        if (subject == null)
    //            return ApiResult<bool>.Fail("Subject not found.");

    //        var isAssigned = await _context.Subjects
    //            .AnyAsync(s => s.SubjectId == id && s.IsActive==true);

    //        if (isAssigned)
    //        {
    //            subject.IsActive = false; // 🔑 soft delete
    //            await _context.SaveChangesAsync();

    //            return ApiResult<bool>.Ok(true, "Subject disabled successfully.");
    //        }

    //        _context.MasterSubjects.Remove(subject);
    //        await _context.SaveChangesAsync();

    //        return ApiResult<bool>.Ok(true, "Subject deleted successfully.");
    //    }

        //public async Task<ApiResult<string>> AddMasterSubjectAsync(AddMasterSubjectRequest req)
        //{
        //    if (string.IsNullOrWhiteSpace(req.SubjectName))
        //        return ApiResult<string>.Fail("Subject name is required");

        //    bool exists = await _context.MasterSubjects.AnyAsync(ms =>
        //        ms.SchoolId == req.SchoolId &&
        //        ms.SubjectName == req.SubjectName);

        //    if (exists)
        //        return ApiResult<string>.Fail("Subject already exists");

        //    var subject = new MasterSubject
        //    {
        //        SchoolId = req.SchoolId,
        //        SubjectName = req.SubjectName.Trim(),
        //        IsActive = true,
                
        //    };

        //    _context.MasterSubjects.Add(subject);
        //    await _context.SaveChangesAsync();

        //    return ApiResult<string>.Ok("Master subject added successfully");
        //}
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
                    s.IsActive==true
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
        public async Task<ApiResult<List<StudentSummaryDto>>> GetStudentsBySchoolAsync(int schoolId)
        {
            var data = await _context.Students
                .Where(s => s.SchoolId == schoolId)
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
                .AsNoTracking() // 🚀 Optimization: Faster read-only query
                .Where(s => s.StudentId == studentId)
                .Select(s => new StudentDetailsDto
                {
                    StudentId = s.StudentId,
                    FullName = s.FirstName + " " + s.LastName,
                    AdmissionNo = s.AdmissionNo,
                    RollNo = s.RollNo,
                    AcademicYear = s.AcademicYear,

                    // 🏢 Dynamic School Name Mapping
                    SchoolName = _context.Schools
                        .Where(sch => sch.SchoolId == s.SchoolId)
                        .Select(sch => sch.SchoolName)
                        .FirstOrDefault() ?? "School",

                    // 🔑 Composite Key Joins
                    ClassName = _context.Classes
                        .Where(c => c.SchoolId == s.SchoolId && c.ClassId == s.ClassId)
                        .Select(c => c.ClassName).FirstOrDefault() ?? "N/A",

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
        public async Task<ApiResult<List<LookUpDto>>> GetExamsOnlyAsync(int schoolId)
        {
            var exams = await _context.Exams.AsNoTracking()
                .Where(e => e.SchoolId == schoolId && e.IsActive==true)
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

        public async Task<ApiResult<List<BulkMarksEntryDto>>> GetMarksEntryListAsync(int examId, int classId, int sectionId, int subjectId, int? streamId)
        {
            var query = _context.Students.AsNoTracking()
                .Where(s => s.ClassId == classId && s.SectionId == sectionId);

            if (streamId.HasValue && streamId > 0)
            {
                query = query.Where(s => s.StreamId == streamId);
            }

            var students = await query
                .OrderBy(s => s.RollNo)
                .Select(s => new BulkMarksEntryDto
                {
                    StudentId = s.StudentId,
                    RollNo = s.RollNo,
                    FullName = s.FirstName + " " + s.LastName,
                    AdmissionNo = s.AdmissionNo,
                    MarksObtained = _context.StudentMarks
                        .Where(m => m.ExamId == examId && m.SubjectId == subjectId && m.StudentId == s.StudentId)
                        .Select(m => (int?)m.MarksObtained).FirstOrDefault(),
                    MaxMarks = _context.ExamSubjects
                        .Where(es => es.ExamId == examId && es.SubjectId == subjectId)
                        .Select(es => es.MaxMarks).FirstOrDefault()
                }).ToListAsync();

            students.ForEach(s => { if (s.MaxMarks == 0) s.MaxMarks = 100; });
            return ApiResult<List<BulkMarksEntryDto>>.Ok(students);
        }

        public async Task<ApiResult<bool>> SaveBulkMarksAsync(BulkSaveRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in request.Marks)
                {
                    var existing = await _context.StudentMarks
                        .FirstOrDefaultAsync(m => m.ExamId == request.ExamId && m.SubjectId == request.SubjectId && m.StudentId == item.StudentId);

                    if (existing != null)
                    {
                        existing.MarksObtained = item.MarksObtained ?? 0;
                    }
                    else
                    {
                        _context.StudentMarks.Add(new StudentMark
                        {
                            SchoolId = request.SchoolId,
                            ExamId = request.ExamId,
                            ClassId = request.ClassId,
                            SubjectId = request.SubjectId,
                            StudentId = item.StudentId,
                            MarksObtained = item.MarksObtained ?? 0,
                            MaxMarks = item.MaxMarks,
                            
                        });
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ApiResult<bool>.Ok(true);
            }
            catch
            {
                await transaction.RollbackAsync();
                return ApiResult<bool>.Fail("Database error while saving.");
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

    }
}






