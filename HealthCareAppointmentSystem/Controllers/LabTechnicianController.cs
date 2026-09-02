using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "LabTechnician")]
    public class LabTechnicianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LabTechnicianController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var orders = await _context.LabOrders
                .Include(lo => lo.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.ApplicationUser)
                .Include(lo => lo.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.ApplicationUser)
                .Include(lo => lo.LabResult)
                .OrderBy(lo => lo.IsCompleted)
                .ThenByDescending(lo => lo.OrderedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BillTest(int id, decimal cost)
        {
            var order = await _context.LabOrders.FindAsync(id);
            if (order == null || order.IsCompleted || order.PaymentStatus != PaymentStatus.Pending) return NotFound();

            order.Cost = cost;
            order.PaymentStatus = PaymentStatus.AwaitingVerification; // Use Unpaid for Processing

            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user!.Id,
                Action = "Lab Test Billed",
                Details = $"Lab Order {id} billed with Cost Rs. {cost}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Test billed successfully.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectPayment(int id, decimal cashAmount = 0)
        {
            var order = await _context.LabOrders
                .Include(lo => lo.Appointment).ThenInclude(a => a!.Patient)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            if (order.PaymentStatus != PaymentStatus.AwaitingVerification) return RedirectToAction(nameof(Dashboard));

            var patientUser = await _userManager.FindByIdAsync(order.Appointment!.Patient!.ApplicationUserId!);
            var patientWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == patientUser!.Id);

            var labTechUser = await _userManager.GetUserAsync(User);
            var labTechWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == labTechUser!.Id);

            var platformWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == null);

            if (patientWallet == null || labTechWallet == null || platformWallet == null)
            {
                TempData["ErrorMessage"] = "Critical wallet error.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (patientWallet.Balance + cashAmount < order.Cost)
            {
                TempData["ErrorMessage"] = $"Patient's wallet has insufficient funds (Balance: Rs. {patientWallet.Balance:N2}). They need Rs. {(order.Cost - cashAmount - patientWallet.Balance):N2} more.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (cashAmount > 0)
            {
                labTechWallet.Balance -= cashAmount;
                patientWallet.Balance += cashAmount;
                
                _context.WalletTransactions.Add(new WalletTransaction { WalletId = labTechWallet.Id, Amount = -cashAmount, Type = TransactionType.ServicePayment, Description = $"Received physical cash for Lab Order #{order.Id}", ReferenceId = $"CASH-LAB-{order.Id}" });
                _context.WalletTransactions.Add(new WalletTransaction { WalletId = patientWallet.Id, Amount = cashAmount, Type = TransactionType.Deposit, Description = $"Cash deposit via Lab Tech for Lab Order #{order.Id}", ReferenceId = $"CASH-LAB-{order.Id}" });
            }

            var totalAmount = order.Cost;
            var platformCommission = totalAmount * 0.10m; // 10% platform fee
            var labEarnings = totalAmount - platformCommission;

            patientWallet.Balance -= totalAmount;
            labTechWallet.Balance += labEarnings;
            platformWallet.Balance += platformCommission;

            _context.WalletTransactions.Add(new WalletTransaction { WalletId = patientWallet.Id, Amount = -totalAmount, Type = TransactionType.ServicePayment, Description = $"Lab Bill for Order #{order.Id}", ReferenceId = $"LAB-{order.Id}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = labTechWallet.Id, Amount = labEarnings, Type = TransactionType.ServiceEarning, Description = $"Earnings from Lab Order #{order.Id}", ReferenceId = $"LAB-{order.Id}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = platformWallet.Id, Amount = platformCommission, Type = TransactionType.PlatformCommission, Description = $"10% Commission from Lab Order #{order.Id}", ReferenceId = $"LAB-{order.Id}" });

            order.PaymentStatus = PaymentStatus.Paid;
            order.SampleId = $"LAB-{DateTime.UtcNow:yyyyMMdd}-{order.Id}";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Payment collected. Sample ID {order.SampleId} generated.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> UploadResult(int id)
        {
            var order = await _context.LabOrders
                .Include(lo => lo.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.ApplicationUser)
                .Include(lo => lo.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || order.IsCompleted) return NotFound("Order not found or already completed.");
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResult(int orderId, string resultNotes, IFormFile resultFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var labTech = await _context.LabTechnicians.FirstOrDefaultAsync(l => l.ApplicationUserId == user.Id);
            if (labTech == null) return NotFound("Lab Technician profile not found.");

            var order = await _context.LabOrders.FindAsync(orderId);
            if (order == null || order.IsCompleted) return NotFound("Order not found or already completed.");

            if (resultFile != null && resultFile.Length > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "labs");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(resultFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resultFile.CopyToAsync(stream);
                }

                var labResult = new LabResult
                {
                    LabOrderId = orderId,
                    LabTechnicianId = labTech.Id,
                    ResultNotes = resultNotes,
                    FileUrl = "/uploads/labs/" + fileName,
                    UploadedAt = DateTime.UtcNow
                };

                _context.LabResults.Add(labResult);
                order.IsCompleted = true;
                
                // Add Audit Log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Lab Result Uploaded",
                    Details = $"Result uploaded for Test {order.TestType} (Appointment #{order.AppointmentId})",
                    UserId = user.Id
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Lab result successfully uploaded!";
            }
            else
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
