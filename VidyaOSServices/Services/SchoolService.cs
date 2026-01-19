using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
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
        public async Task<ApiResult<RegisterSchoolResponse>> RegisterSchoolAsync(
    RegisterSchoolRequest req)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // ---------- VALIDATION ----------
                if (string.IsNullOrWhiteSpace(req.SchoolName))
                    return ApiResult<RegisterSchoolResponse>.Fail("School name is required.");

                if (string.IsNullOrWhiteSpace(req.SchoolCode))
                    return ApiResult<RegisterSchoolResponse>.Fail("School code is required.");

                if (string.IsNullOrWhiteSpace(req.AdminUsername))
                    return ApiResult<RegisterSchoolResponse>.Fail("Admin username is required.");

                if (string.IsNullOrWhiteSpace(req.AdminPassword))
                    return ApiResult<RegisterSchoolResponse>.Fail("Admin password is required.");

                if (!Regex.IsMatch(req.Phone, @"^[6-9]\d{9}$"))
                    return ApiResult<RegisterSchoolResponse>.Fail("Invalid phone number.");

                // ---------- DUPLICATE CHECKS ----------
                if (await _context.Schools.AnyAsync(s => s.SchoolCode == req.SchoolCode))
                    return ApiResult<RegisterSchoolResponse>.Fail("School code already exists.");

                if (await _context.Users.AnyAsync(u => u.Username == req.AdminUsername))
                    return ApiResult<RegisterSchoolResponse>.Fail("Admin username already exists.");

                // ---------- CREATE SCHOOL ----------
                var school = new School
                {
                    SchoolName = req.SchoolName.Trim(),
                    SchoolCode = req.SchoolCode.Trim().ToUpper(),
                    RegistrationNumber = req.RegistrationNumber,
                    YearOfFoundation = req.YearOfFoundation,
                    BoardType = req.BoardType,
                    AffiliationNumber = req.AffiliationNumber,
                    Email = req.Email,
                    Phone = req.Phone,
                    AddressLine1 = req.AddressLine1,
                    City = req.City,
                    State = req.State,
                    Pincode = req.Pincode,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Schools.Add(school);
                await _context.SaveChangesAsync();

                // ---------- CREATE ADMIN USER ----------
                var adminUser = new User
                {
                    SchoolId = school.SchoolId, // 🔥 RELATION
                    Username = req.AdminUsername.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.AdminPassword),
                    Role = "SchoolAdmin",
                    Email = req.Email,
                    Phone = req.Phone,
                    IsFirstLogin = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return ApiResult<RegisterSchoolResponse>.Ok(
                    new RegisterSchoolResponse
                    {
                        SchoolId = school.SchoolId,
                        SchoolName = school.SchoolName!,      // ✅ FIX
                        SchoolCode = school.SchoolCode!,
                        AdminUserId = adminUser.UserId,
                        AdminUsername = adminUser.Username!,
                        CreatedAt = school.CreatedAt!.Value
                    },
                    "School registered successfully."
                );
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<AttendanceViewResponse> ViewAttendanceAsync(
            int schoolId,
            int classId,
            int sectionId,
            DateOnly date)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // 🚫 Future date safeguard
            //if (date > today)
            //{
            //    return new AttendanceViewResponse
            //    {
            //        AttendanceDate = date,
            //        AttendanceTaken = false,
            //        Summary = new AttendanceSummary(),
            //        Students = new List<AttendanceViewStudentDto>()
            //    };
            //}

            // 1️⃣ Students of class + section
            var students = await _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.SectionId == sectionId &&
                    s.IsActive == true)
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
                    Message = "No students found for selected class and section",
                    AttendanceDate = date
                };
            }

            var userIds = students.Select(s => s.UserId).ToList();

            // 2️⃣ Approved leave for date
            var leaveUserIds = await _context.Leaves
                .Where(l =>
                    l.SchoolId == schoolId &&
                    l.Status == "Approved" &&
                    date >= l.FromDate &&
                    date <= l.ToDate)
                .Select(l => l.UserId)
                .ToListAsync();

            // 3️⃣ Attendance only for these students
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
                // 🏖️ Leave overrides everything
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
        public async Task<ApiResult<string>> GenerateMonthlyFeesAsync(
    int schoolId,
    string feeMonth   // format: yyyy-MM
)
        {
            if (schoolId <= 0 || string.IsNullOrWhiteSpace(feeMonth))
                return ApiResult<string>.Fail("Invalid input.");

            // 1️⃣ Load all active students
            var students = await _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.IsActive == true)
                .ToListAsync();

            foreach (var student in students)
            {
                // 2️⃣ Skip if fee already generated
                bool exists = await _context.StudentFees.AnyAsync(f =>
                    f.StudentId == student.StudentId &&
                    f.FeeMonth == feeMonth);

                if (exists)
                    continue;

                // 3️⃣ Find fee structure
                var feeStructure = await _context.FeeStructures
                    .FirstOrDefaultAsync(f =>
                        f.SchoolId == schoolId &&
                        f.ClassId == student.ClassId &&
                        f.IsActive == true &&
                        (
                            student.ClassId < 11 ||
                            f.StreamId == student.StreamId
                        )
                    );

                if (feeStructure == null)
                    continue;

                // 4️⃣ Create student fee
                _context.StudentFees.Add(new StudentFee
                {
                    StudentId = student.StudentId,
                    FeeMonth = feeMonth,
                    Amount = feeStructure.MonthlyAmount,
                    Status = "Pending"
                });
            }

            await _context.SaveChangesAsync();

            return ApiResult<string>.Ok("Fees generated successfully.");
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


        public async Task<ApiResult<FeeStructureResponse>> SaveFeeStructureAsync(
     FeeStructureRequest req)
        {
            // 🔴 BASIC VALIDATIONS
            if (req == null)
                return ApiResult<FeeStructureResponse>.Fail("Request is required.");

            if (req.SchoolId <= 0)
                return ApiResult<FeeStructureResponse>.Fail("Invalid school.");

            if (req.ClassId <= 0)
                return ApiResult<FeeStructureResponse>.Fail("Invalid class.");

            if (string.IsNullOrWhiteSpace(req.FeeName))
                return ApiResult<FeeStructureResponse>.Fail("Fee name is required.");

            if (req.MonthlyAmount <= 0)
                return ApiResult<FeeStructureResponse>.Fail("Monthly amount must be greater than zero.");

            // 🔴 STREAM RULE
            // Class 11 & 12 → stream REQUIRED
            if ((req.ClassId == 11 || req.ClassId == 12) && req.StreamId == null)
                return ApiResult<FeeStructureResponse>.Fail(
                    "Stream is required for class 11 and 12."
                );

            // Class 1–10 → stream MUST be null
            if (req.ClassId < 11)
                req.StreamId = null;

            // 🔍 CHECK EXISTING FEE (School + Class + Stream)
            var existingFee = await _context.FeeStructures
                .FirstOrDefaultAsync(f =>
                    f.SchoolId == req.SchoolId &&
                    f.ClassId == req.ClassId &&
                    f.StreamId == req.StreamId &&
                    f.IsActive == true
                );

            // 🔁 UPDATE
            if (existingFee != null)
            {
                existingFee.FeeName = req.FeeName;
                existingFee.MonthlyAmount = req.MonthlyAmount;

                await _context.SaveChangesAsync();

                return ApiResult<FeeStructureResponse>.Ok(
                    new FeeStructureResponse
                    {
                        FeeStructureId = existingFee.FeeStructureId,
                        ClassId = existingFee.ClassId!.Value,
                        StreamId = existingFee.StreamId,
                        FeeName = existingFee.FeeName!,
                        MonthlyAmount = existingFee.MonthlyAmount!.Value,
                        IsActive = true
                    },
                    "Fee structure updated successfully."
                );
            }

            // ➕ CREATE NEW
            var fee = new FeeStructure
            {
                SchoolId = req.SchoolId,
                ClassId = req.ClassId,
                StreamId = req.StreamId, // null for 1–10, value for 11–12
                FeeName = req.FeeName,
                MonthlyAmount = req.MonthlyAmount,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.FeeStructures.Add(fee);
            await _context.SaveChangesAsync();

            return ApiResult<FeeStructureResponse>.Ok(
                new FeeStructureResponse
                {
                    FeeStructureId = fee.FeeStructureId,
                    ClassId = fee.ClassId!.Value,
                    StreamId = fee.StreamId,
                    FeeName = fee.FeeName!,
                    MonthlyAmount = fee.MonthlyAmount!.Value,
                    IsActive = true
                },
                "Fee structure created successfully."
            );
        }


        public async Task<ApiResult<List<FeeStructureListResponse>>> GetFeeStructuresAsync(
    int schoolId)
        {
            var data = await (
                from fs in _context.FeeStructures
                join c in _context.Classes on fs.ClassId equals c.ClassId
                where fs.SchoolId == schoolId && fs.IsActive == true
                orderby c.ClassName
                select new FeeStructureListResponse
                {
                    FeeStructureId = fs.FeeStructureId,
                    ClassId = (int)fs.ClassId,
                    ClassName = c.ClassName!,
                    FeeName = fs.FeeName!,
                    MonthlyAmount = (decimal)fs.MonthlyAmount,
                    IsActive = fs.IsActive ?? false
                }
            ).ToListAsync();

            return ApiResult<List<FeeStructureListResponse>>.Ok(data);
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
    int sectionId)
        {
            var students = await _context.Students
                .Where(s =>
                    s.SchoolId == schoolId &&
                    s.ClassId == classId &&
                    s.SectionId == sectionId &&
                    s.IsActive == true
                )
                .OrderBy(s => s.RollNo)
                .Select(s => new StudentListDto
                {
                    StudentId = s.StudentId,
                    AdmissionNo = s.AdmissionNo!,
                    FullName = s.FirstName + " " + s.LastName,
                    RollNo = s.RollNo ?? 0
                })
                .ToListAsync();

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




    }
}





