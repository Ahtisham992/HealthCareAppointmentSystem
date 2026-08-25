using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InvoicesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Appointment).ThenInclude(a => a!.Doctor).ThenInclude(d => d!.ApplicationUser)
                .Include(i => i.Appointment).ThenInclude(a => a!.Patient).ThenInclude(p => p!.ApplicationUser)
                .Include(i => i.Appointment).ThenInclude(a => a!.Doctor!.Specialization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            if (!await UserCanAccessInvoice(invoice)) return Forbid();

            return View(invoice);
        }

        // POST: /Invoices/PayWithWallet/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> PayWithWallet(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Appointment).ThenInclude(a => a!.Doctor)
                .Include(i => i.Appointment).ThenInclude(a => a!.Patient)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            if (!await UserCanAccessInvoice(invoice)) return Forbid();
            if (invoice.Status == PaymentStatus.Paid) return RedirectToAction(nameof(Details), new { id = invoice.Id });

            var currentUser = await _userManager.GetUserAsync(User);
            var patientWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == currentUser.Id);
            var doctorUser = await _userManager.FindByIdAsync(invoice.Appointment!.Doctor!.ApplicationUserId!);
            var doctorWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == doctorUser!.Id);
            var platformWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == null);

            if (patientWallet == null || doctorWallet == null || platformWallet == null)
            {
                TempData["Error"] = "Critical wallet error. Please contact support.";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            if (patientWallet.Balance < invoice.Amount)
            {
                TempData["Error"] = $"Insufficient wallet balance. You need Rs. {invoice.Amount:N2} but have Rs. {patientWallet.Balance:N2}. Please add funds to your wallet.";
                return RedirectToAction("Index", "Wallet");
            }

            // Calculate Split
            var totalAmount = invoice.Amount;
            var platformCommission = totalAmount * 0.10m; // 10%
            var doctorEarnings = totalAmount - platformCommission;

            // Apply transactions
            patientWallet.Balance -= totalAmount;
            doctorWallet.Balance += doctorEarnings;
            platformWallet.Balance += platformCommission;

            _context.WalletTransactions.Add(new WalletTransaction { WalletId = patientWallet.Id, Amount = -totalAmount, Type = TransactionType.ServicePayment, Description = $"Payment for Appointment #{invoice.AppointmentId}", ReferenceId = $"APP-{invoice.AppointmentId}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = doctorWallet.Id, Amount = doctorEarnings, Type = TransactionType.ServiceEarning, Description = $"Earnings from Appointment #{invoice.AppointmentId}", ReferenceId = $"APP-{invoice.AppointmentId}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = platformWallet.Id, Amount = platformCommission, Type = TransactionType.PlatformCommission, Description = $"10% Commission from Appointment #{invoice.AppointmentId}", ReferenceId = $"APP-{invoice.AppointmentId}" });

            invoice.Status = PaymentStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaymentMethod = "Digital Wallet";
            
            if (invoice.Appointment != null && invoice.Appointment.Status == AppointmentStatus.Pending)
            {
                invoice.Appointment.Status = AppointmentStatus.Confirmed;
            }

            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Successfully paid Rs. {totalAmount:N2} via Digital Wallet.";
            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }

        // POST: /Invoices/CollectCashPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Receptionist")]
        public async Task<IActionResult> CollectCashPayment(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Appointment).ThenInclude(a => a!.Doctor)
                .Include(i => i.Appointment).ThenInclude(a => a!.Patient)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            if (invoice.Status == PaymentStatus.Paid) return RedirectToAction(nameof(Details), new { id = invoice.Id });

            var currentUser = await _userManager.GetUserAsync(User);
            var receptionist = await _context.Receptionists.FirstOrDefaultAsync(r => r.ApplicationUserId == currentUser!.Id);
            
            var patientUser = await _userManager.FindByIdAsync(invoice.Appointment!.Patient!.ApplicationUserId!);
            var patientWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == patientUser!.Id);
            var doctorUser = await _userManager.FindByIdAsync(invoice.Appointment!.Doctor!.ApplicationUserId!);
            var doctorWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == doctorUser!.Id);
            var platformWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.ApplicationUserId == null);

            if (patientWallet == null || doctorWallet == null || platformWallet == null || receptionist == null)
            {
                TempData["Error"] = "Critical wallet/receptionist error.";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            // 1. Receptionist takes physical cash. Add to CashDrawer.
            receptionist.CashDrawerBalance += invoice.Amount;

            // 2. Deposit digital equivalent to Patient Wallet
            patientWallet.Balance += invoice.Amount;
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = patientWallet.Id, Amount = invoice.Amount, Type = TransactionType.CashDeposit, Description = "Cash Deposit by Receptionist", ReferenceId = $"CASH-{invoice.Id}" });

            // 3. Process Payment (Split)
            var totalAmount = invoice.Amount;
            var platformCommission = totalAmount * 0.10m;
            var doctorEarnings = totalAmount - platformCommission;

            patientWallet.Balance -= totalAmount;
            doctorWallet.Balance += doctorEarnings;
            platformWallet.Balance += platformCommission;

            _context.WalletTransactions.Add(new WalletTransaction { WalletId = patientWallet.Id, Amount = -totalAmount, Type = TransactionType.ServicePayment, Description = $"Payment for Appointment #{invoice.AppointmentId}", ReferenceId = $"APP-{invoice.AppointmentId}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = doctorWallet.Id, Amount = doctorEarnings, Type = TransactionType.ServiceEarning, Description = $"Earnings from Appointment #{invoice.AppointmentId}", ReferenceId = $"APP-{invoice.AppointmentId}" });
            _context.WalletTransactions.Add(new WalletTransaction { WalletId = platformWallet.Id, Amount = platformCommission, Type = TransactionType.PlatformCommission, Description = $"10% Commission from Appointment #{invoice.AppointmentId}", ReferenceId = $"APP-{invoice.AppointmentId}" });

            invoice.Status = PaymentStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.PaymentMethod = "Cash at Desk";
            invoice.CollectedByReceptionistId = receptionist.Id;
            
            if (invoice.Appointment != null && invoice.Appointment.Status == AppointmentStatus.Pending)
            {
                invoice.Appointment.Status = AppointmentStatus.Confirmed;
            }

            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Cash collected and payment successfully processed via Ledger.";
            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }


        private async Task<bool> UserCanAccessInvoice(Invoice invoice)
        {
            if (User.IsInRole("Admin")) return true;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return false;

            if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == currentUser.Id);
                return doctor != null && doctor.Id == invoice.Appointment!.DoctorId;
            }

            if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id);
                return patient != null && patient.Id == invoice.Appointment!.PatientId;
            }

            if (User.IsInRole("Receptionist"))
            {
                return true; // Receptionists handle payments, so they need access to view all invoices.
            }

            return false;
        }
    }
}
