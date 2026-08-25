using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.ViewModels;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class EarningsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const decimal PLATFORM_COMMISSION_RATE = 0.10m; // 10%

        public EarningsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Earnings/AdminIndex
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var doctors = await _context.Users
                .Join(_context.Set<Doctor>(), u => u.Id, d => d.ApplicationUserId, (u, d) => new { User = u, Doctor = d })
                .ToListAsync();

            var viewModel = new AdminEarningsIndexViewModel
            {
                Month = selectedMonth,
                Year = selectedYear
            };

            foreach (var doc in doctors)
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Appointment)
                    .Where(i => i.Appointment.DoctorId == doc.Doctor.Id &&
                                ((i.Status == PaymentStatus.Paid && i.PaidAt.HasValue && i.PaidAt.Value.Month == selectedMonth && i.PaidAt.Value.Year == selectedYear) ||
                                 (i.Status == PaymentStatus.Refunded && i.PaidAt.HasValue && i.PaidAt.Value.Month == selectedMonth && i.PaidAt.Value.Year == selectedYear)))
                    .ToListAsync();

                // Refunded invoices were paid initially, so they still count towards Total Earned before being subtracted
                decimal totalEarned = invoices.Where(i => i.Status == PaymentStatus.Paid || i.Status == PaymentStatus.Refunded).Sum(i => i.Amount);
                decimal totalRefunded = invoices.Where(i => i.Status == PaymentStatus.Refunded).Sum(i => i.RefundAmount ?? i.Amount);
                decimal netEarned = totalEarned - totalRefunded;

                var existingBills = await _context.PlatformBills
                    .Where(b => b.DoctorId == doc.Doctor.Id && b.Month == selectedMonth && b.Year == selectedYear)
                    .OrderByDescending(b => b.GeneratedAt)
                    .ToListAsync();

                decimal billedEarnings = existingBills.Sum(b => b.TotalEarnings);
                decimal billedCommission = existingBills.Sum(b => b.CommissionAmount);
                decimal unbilledEarnings = Math.Max(0, netEarned - billedEarnings);
                decimal unbilledCommission = Math.Max(0, (netEarned > 0 ? netEarned * PLATFORM_COMMISSION_RATE : 0) - billedCommission);

                var latestBill = existingBills.FirstOrDefault();

                var row = new DoctorEarningRow
                {
                    DoctorId = doc.Doctor.Id,
                    DoctorName = "Dr. " + doc.User.FullName,
                    TotalEarned = totalEarned,
                    TotalRefunded = totalRefunded,
                    NetEarned = netEarned,
                    Commission = netEarned > 0 ? netEarned * PLATFORM_COMMISSION_RATE : 0,
                    BilledEarnings = billedEarnings,
                    BilledCommission = billedCommission,
                    UnbilledEarnings = unbilledEarnings,
                    UnbilledCommission = unbilledCommission,
                    HasPendingBills = existingBills.Any(b => b.Status == BillStatus.Pending),
                    HasSubmittedBills = existingBills.Any(b => b.Status == BillStatus.PaymentSubmitted),
                    LatestBillStatus = latestBill?.Status,
                    LatestBillId = latestBill?.Id
                };

                viewModel.Doctors.Add(row);
                viewModel.TotalPlatformEarnings += row.Commission;
            }

            return View(viewModel);
        }

        // POST: Earnings/GenerateBill
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBill(int doctorId, int month, int year, decimal totalEarnings, decimal commissionAmount)
        {
            // We allow multiple bills per month now to handle incremental earnings
            // Just verifying we are not generating a bill for 0 earnings
            if (totalEarnings <= 0 || commissionAmount <= 0)
            {
                TempData["Error"] = "Cannot generate a bill for zero earnings.";
                return RedirectToAction(nameof(AdminIndex), new { month, year });
            }

            var bill = new PlatformBill
            {
                DoctorId = doctorId,
                Month = month,
                Year = year,
                TotalEarnings = totalEarnings,
                CommissionAmount = commissionAmount,
                Status = BillStatus.Pending,
                GeneratedAt = DateTime.UtcNow
            };

            _context.PlatformBills.Add(bill);
            
            var doctor = await _context.Set<Doctor>().Include(d => d.ApplicationUser).FirstOrDefaultAsync(d => d.Id == doctorId);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "BillGenerated",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Details = $"Generated platform bill of Rs. {commissionAmount:N0} for Dr. {doctor?.ApplicationUser?.FullName} for {month}/{year}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Platform bill generated successfully.";
            return RedirectToAction(nameof(AdminIndex), new { month, year });
        }

        // POST: Earnings/SubmitBillPayment
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBillPayment(int id, string paymentMethod, string transactionReference)
        {
            var bill = await _context.PlatformBills.FindAsync(id);
            if (bill == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Set<Doctor>().Include(d => d.ApplicationUser).FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            
            if (doctor == null || bill.DoctorId != doctor.Id) return Unauthorized();

            if (bill.Status != BillStatus.Pending)
            {
                TempData["Error"] = "Bill is not in a state to accept payment submissions.";
                return RedirectToAction(nameof(MyEarnings), new { month = bill.Month, year = bill.Year });
            }

            bill.Status = BillStatus.PaymentSubmitted;
            bill.PaymentMethod = paymentMethod;
            bill.TransactionReference = transactionReference;
            bill.SubmittedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "BillPaymentSubmitted",
                UserId = userId,
                Details = $"Doctor submitted payment for bill ID {id}. TrxRef: {transactionReference}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Payment submitted successfully. Awaiting admin verification.";
            return RedirectToAction(nameof(MyEarnings), new { month = bill.Month, year = bill.Year });
        }

        // POST: Earnings/MarkBillPaid
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkBillPaid(int id)
        {
            var bill = await _context.PlatformBills.FindAsync(id);
            if (bill == null) return NotFound();

            bill.Status = BillStatus.Paid;
            bill.PaidAt = DateTime.UtcNow;

            var doctor = await _context.Set<Doctor>().Include(d => d.ApplicationUser).FirstOrDefaultAsync(d => d.Id == bill.DoctorId);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "BillVerified",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Details = $"Admin verified and marked platform bill ID {id} as Paid for Dr. {doctor?.ApplicationUser?.FullName}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Payment verified and marked as paid.";
            return RedirectToAction(nameof(AdminIndex), new { month = bill.Month, year = bill.Year });
        }

        // GET: Earnings/MyEarnings
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> MyEarnings(int? month, int? year)
        {
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Set<Doctor>().FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            
            if (doctor == null) return NotFound();

            var invoices = await _context.Invoices
                .Include(i => i.Appointment)
                .Where(i => i.Appointment.DoctorId == doctor.Id &&
                            ((i.Status == PaymentStatus.Paid && i.PaidAt.HasValue && i.PaidAt.Value.Month == selectedMonth && i.PaidAt.Value.Year == selectedYear) ||
                             (i.Status == PaymentStatus.Refunded && i.PaidAt.HasValue && i.PaidAt.Value.Month == selectedMonth && i.PaidAt.Value.Year == selectedYear)))
                .OrderByDescending(i => i.PaidAt)
                .ToListAsync();

            var viewModel = new DoctorEarningsViewModel
            {
                Month = selectedMonth,
                Year = selectedYear
            };

            foreach (var invoice in invoices)
            {
                if (invoice.Status == PaymentStatus.Paid || invoice.Status == PaymentStatus.Refunded)
                {
                    // Every paid or refunded invoice was earned initially
                    viewModel.Logs.Add(new EarningLogViewModel
                    {
                        Date = invoice.PaidAt.Value,
                        Description = $"Payment received for Appointment #{invoice.Appointment.Id}",
                        Amount = invoice.Amount,
                        Type = "Payment"
                    });
                    viewModel.TotalEarnedThisMonth += invoice.Amount;
                }
                
                if (invoice.Status == PaymentStatus.Refunded)
                {
                    var refundAmount = invoice.RefundAmount ?? invoice.Amount;
                    viewModel.Logs.Add(new EarningLogViewModel
                    {
                        Date = invoice.PaidAt.Value, // We don't have a dedicated RefundedAt field, using PaidAt
                        Description = $"Refund processed for Appointment #{invoice.Appointment.Id}",
                        Amount = refundAmount,
                        Type = "Refund"
                    });
                    viewModel.TotalRefundedThisMonth += refundAmount;
                }
            }

            var platformBills = await _context.PlatformBills
                .Where(b => b.DoctorId == doctor.Id)
                .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
                .ToListAsync();
                
            var monthlyBills = platformBills.Where(b => b.Month == selectedMonth && b.Year == selectedYear).ToList();
            foreach (var monthlyBill in monthlyBills)
            {
                if (monthlyBill.Status == BillStatus.Paid || monthlyBill.Status == BillStatus.PaymentSubmitted)
                {
                    viewModel.TotalPlatformFeesPaid += monthlyBill.CommissionAmount;
                    viewModel.Logs.Add(new EarningLogViewModel
                    {
                        Date = monthlyBill.SubmittedAt ?? (monthlyBill.PaidAt ?? DateTime.UtcNow),
                        Description = $"Platform Fee Payment (Bill #{monthlyBill.Id})",
                        Amount = monthlyBill.CommissionAmount,
                        Type = "Platform Fee"
                    });
                }
            }
            
            viewModel.NetEarnedThisMonth = viewModel.TotalEarnedThisMonth - viewModel.TotalRefundedThisMonth - viewModel.TotalPlatformFeesPaid;

            ViewBag.Bills = platformBills;

            return View(viewModel);
        }

        // GET: Earnings/ViewBill/5
        public async Task<IActionResult> ViewBill(int id)
        {
            var bill = await _context.PlatformBills
                .Include(b => b.Doctor)
                .ThenInclude(d => d.ApplicationUser)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Doctor") && bill.Doctor.ApplicationUserId != userId)
            {
                return Unauthorized();
            }

            return View(bill);
        }
    }
}
