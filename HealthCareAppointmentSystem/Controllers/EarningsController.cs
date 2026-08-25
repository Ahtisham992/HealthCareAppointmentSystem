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

                var existingBill = await _context.PlatformBills
                    .FirstOrDefaultAsync(b => b.DoctorId == doc.Doctor.Id && b.Month == selectedMonth && b.Year == selectedYear);

                var row = new DoctorEarningRow
                {
                    DoctorId = doc.Doctor.Id,
                    DoctorName = "Dr. " + doc.User.FullName,
                    TotalEarned = totalEarned,
                    TotalRefunded = totalRefunded,
                    NetEarned = netEarned,
                    Commission = netEarned > 0 ? netEarned * PLATFORM_COMMISSION_RATE : 0,
                    IsBillGenerated = existingBill != null,
                    BillStatus = existingBill?.Status,
                    BillId = existingBill?.Id
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
            var existingBill = await _context.PlatformBills
                .FirstOrDefaultAsync(b => b.DoctorId == doctorId && b.Month == month && b.Year == year);

            if (existingBill != null)
            {
                TempData["Error"] = "Bill already generated for this month.";
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
                Action = "BillPaid",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Details = $"Marked platform bill ID {id} as Paid for Dr. {doctor?.ApplicationUser?.FullName}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Bill marked as paid.";
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

            viewModel.NetEarnedThisMonth = viewModel.TotalEarnedThisMonth - viewModel.TotalRefundedThisMonth;

            ViewBag.Bills = await _context.PlatformBills
                .Where(b => b.DoctorId == doctor.Id)
                .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
                .ToListAsync();

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
