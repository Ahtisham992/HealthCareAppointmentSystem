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

        // GET: /Invoices/SubmitPayment/5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> SubmitPayment(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Appointment)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            if (!await UserCanAccessInvoice(invoice)) return Forbid();

            if (invoice.Status == PaymentStatus.Paid)
            {
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            return View(invoice);
        }

        // POST: /Invoices/SubmitPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> SubmitPayment(int id, string paymentMethod, string transactionReference, IFormFile? screenshot)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Appointment)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            if (!await UserCanAccessInvoice(invoice)) return Forbid();

            if (string.IsNullOrWhiteSpace(paymentMethod) || string.IsNullOrWhiteSpace(transactionReference))
            {
                ModelState.AddModelError("", "Payment Method and Transaction Reference are required.");
                return View(invoice);
            }

            if (screenshot == null || screenshot.Length == 0)
            {
                ModelState.AddModelError("", "Payment screenshot is required.");
                return View(invoice);
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "screenshots");
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + screenshot.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await screenshot.CopyToAsync(stream);
            }

            invoice.PaymentScreenshotUrl = "/uploads/screenshots/" + uniqueFileName;
            invoice.PaymentMethod = paymentMethod;
            invoice.TransactionReference = transactionReference;
            invoice.Status = PaymentStatus.AwaitingVerification;
            
            var currentUser = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Submitted Payment Reference",
                UserId = currentUser?.Id ?? "System",
                Details = $"Invoice #{invoice.Id} marked AwaitingVerification. Ref: {transactionReference}"
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }

        // POST: /Invoices/VerifyPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> VerifyPayment(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Appointment)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            if (!await UserCanAccessInvoice(invoice)) return Forbid();

            invoice.Status = PaymentStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;

            if (invoice.Appointment != null && invoice.Appointment.Status == AppointmentStatus.Pending)
            {
                invoice.Appointment.Status = AppointmentStatus.Confirmed;
            }

            var currentUser = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Verified Payment",
                UserId = currentUser?.Id ?? "System",
                Details = $"Invoice #{invoice.Id} marked as Paid."
            });

            await _context.SaveChangesAsync();

            // Redirect back to dashboard or invoice details
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

            return false;
        }
    }
}
