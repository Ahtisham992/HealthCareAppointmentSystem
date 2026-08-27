using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Pharmacist")]
    public class PharmacistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PharmacistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var pharmacist = await _context.Pharmacists.FirstOrDefaultAsync(p => p.ApplicationUserId == user!.Id);
            
            if (pharmacist == null) return Unauthorized();

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.ApplicationUser)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a!.Patient)
                        .ThenInclude(pat => pat!.ApplicationUser)
                .Include(p => p.Items)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(prescriptions);
        }

        [HttpGet]
        public async Task<IActionResult> Process(int id)
        {
            var p = await _context.Prescriptions
                .Include(p => p.Appointment).ThenInclude(a => a!.Patient).ThenInclude(pat => pat!.ApplicationUser)
                .Include(p => p.Appointment).ThenInclude(a => a!.Doctor).ThenInclude(d => d!.ApplicationUser)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return NotFound();
            if (p.Status != PrescriptionStatus.Pending) return RedirectToAction("Index");

            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int id, Dictionary<int, decimal> ItemPrices)
        {
            var p = await _context.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
            if (p == null) return NotFound();

            decimal total = 0;
            foreach (var item in p.Items)
            {
                if (ItemPrices.TryGetValue(item.Id, out decimal price))
                {
                    item.Price = price;
                    total += price;
                }
            }

            p.TotalAmount = total;
            p.Status = PrescriptionStatus.Processing;
            
            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user!.Email,
                Action = "Prescription Processed",
                Details = $"Prescription {id} processed with Total Rs. {total}."
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Prescription billed and processing started.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectPayment(int id, decimal cashAmount = 0)
        {
            var p = await _context.Prescriptions
                .Include(p => p.Appointment).ThenInclude(a => a!.Patient)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return NotFound();
            if (p.Status != PrescriptionStatus.Processing) return RedirectToAction("Index");

            var patientUser = await _userManager.FindByIdAsync(p.Appointment!.Patient!.ApplicationUserId!);
            var patientWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == patientUser!.Id);

            var pharmacistUser = await _userManager.GetUserAsync(User);
            var pharmacistWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == pharmacistUser!.Id);

            var platformWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == null);

            if (patientWallet == null || pharmacistWallet == null || platformWallet == null)
            {
                TempData["Error"] = "Critical wallet error.";
                return RedirectToAction("Index");
            }

            if (patientWallet.Balance + cashAmount < p.TotalAmount)
            {
                TempData["Error"] = $"Patient's wallet has insufficient funds (Balance: Rs. {patientWallet.Balance:N2}). They need Rs. {(p.TotalAmount - cashAmount - patientWallet.Balance):N2} more.";
                return RedirectToAction("Index");
            }

            if (cashAmount > 0)
            {
                // Pharmacist takes physical cash. Pharmacist's digital wallet is deducted, Patient's digital wallet is credited.
                pharmacistWallet.Balance -= cashAmount;
                patientWallet.Balance += cashAmount;
                
                _context.WalletTransactions.Add(new WalletTransaction { WalletId = pharmacistWallet.Id, Amount = -cashAmount, Type = TransactionType.ServicePayment, Description = $"Received physical cash for RX #{p.Id}", ReferenceId = $"CASH-{p.Id}" });
                _context.WalletTransactions.Add(new WalletTransaction { WalletId = patientWallet.Id, Amount = cashAmount, Type = TransactionType.Deposit, Description = $"Cash deposit via Pharmacist for RX #{p.Id}", ReferenceId = $"CASH-{p.Id}" });
            }

            // Calculate Split
            var totalAmount = p.TotalAmount;
            var platformCommission = totalAmount * 0.05m; // 5% pharmacy platform fee
            var pharmacyEarnings = totalAmount - platformCommission;

            // Apply digital transactions
            patientWallet.Balance -= totalAmount;
            pharmacistWallet.Balance += pharmacyEarnings;
            platformWallet.Balance += platformCommission;

            _context.WalletTransactions.Add(new WalletTransaction { WalletId = patientWallet.Id, Amount = -totalAmount, Type = TransactionType.ServicePayment, Description = $"Pharmacy Bill for RX #{p.Id}", ReferenceId = $"RX-{p.Id}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = pharmacistWallet.Id, Amount = pharmacyEarnings, Type = TransactionType.ServiceEarning, Description = $"Earnings from RX #{p.Id}", ReferenceId = $"RX-{p.Id}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = platformWallet.Id, Amount = platformCommission, Type = TransactionType.PlatformCommission, Description = $"5% Commission from RX #{p.Id}", ReferenceId = $"RX-{p.Id}" });

            p.PaymentStatus = PaymentStatus.Paid;
            p.PaidAt = DateTime.UtcNow;
            p.Status = PrescriptionStatus.Dispensed;
            p.DispensedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = pharmacistUser.Email,
                Action = "Prescription Paid & Dispensed",
                Details = $"Prescription {id} paid via Wallet and dispensed."
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Payment collected (Cash: Rs. {cashAmount:N2}, Wallet: Rs. {(totalAmount - cashAmount):N2}) and medicines dispensed.";
            return RedirectToAction("Index");
        }
    }
}
