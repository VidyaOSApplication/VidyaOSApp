using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;

namespace VidyaOSServices.Services
{
    public class SubscriptionService
    {
        private readonly VidyaOsContext _context;

        public SubscriptionService(VidyaOsContext context)
        {
            _context = context;
        }

        // 🚀 NEW: Get all active plans for the SuperAdmin UI
        public async Task<ApiResult<List<SubscriptionPlan>>> GetAvailablePlansAsync()
        {
            try
            {
                var plans = await _context.SubscriptionPlans
                    .Where(p => p.IsActive == true)
                    .ToListAsync();

                return ApiResult<List<SubscriptionPlan>>.Ok(plans);
            }
            catch (Exception ex)
            {
                return ApiResult<List<SubscriptionPlan>>.Fail("Error fetching plans: " + ex.Message);
            }
        }

        // 🚀 EXISTING: Activate plan with Yearly logic included
        public async Task<ApiResult<bool>> ManuallyActivateSubscriptionAsync(int schoolId, int planId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(planId);
                if (plan == null) return ApiResult<bool>.Fail("Plan not found.");

                // Deactivate old plans
                var currentSubs = await _context.Subscriptions
                    .Where(s => s.SchoolId == schoolId && s.IsActive == true)
                    .ToListAsync();
                foreach (var sub in currentSubs) sub.IsActive = false;

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                DateOnly expiryDate = plan.PlanName switch
                {
                    "Trial" => today.AddDays(15),
                    "Yearly" => today.AddYears(1),
                    _ => today.AddMonths(1)
                };

                var newSub = new Subscription
                {
                    SchoolId = schoolId,
                    PlanId = planId,
                    StartDate = today,
                    EndDate = expiryDate,
                    IsTrial = (plan.PlanName == "Trial"),
                    IsActive = true
                };
                _context.Subscriptions.Add(newSub);
                await _context.SaveChangesAsync();

                // Log manual payment
                _context.SubscriptionPayments.Add(new SubscriptionPayment
                {
                    SchoolId = schoolId,
                    SubscriptionId = newSub.SubscriptionId,
                    Amount = plan.PriceMonthly,
                    PaymentStatus = "Completed",
                    PaymentGateway = "MANUAL_ENTRY",
                    PaymentDate = DateTime.UtcNow,
                    BillingCycle = plan.PlanName
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return ApiResult<bool>.Ok(true, $"{plan.PlanName} activated until {expiryDate}");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ApiResult<bool>.Fail("Activation failed: " + ex.Message);
            }
        }

            public async Task<ApiResult<List<SchoolListDto>>> GetAllSchoolsAsync()
        {
            try
            {
                // Retrieves schools from the database
                var schools = await _context.Schools
                    .Select(s => new SchoolListDto
                    {
                        SchoolId = s.SchoolId,
                        SchoolName = s.SchoolName,
                        SchoolCode = s.SchoolCode,
                        IsActive = s.IsActive ?? false
                    })
                    .OrderBy(s => s.SchoolName)
                    .ToListAsync();

                return ApiResult<List<SchoolListDto>>.Ok(schools);
            }
            catch (Exception ex)
            {
                return ApiResult<List<SchoolListDto>>.Fail("Error fetching school list: " + ex.Message);
            }
        }

        // Simple DTO for the list
        
    }
    }

