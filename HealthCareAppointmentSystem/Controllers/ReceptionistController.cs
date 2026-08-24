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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReceptionistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var receptionist = await _context.Receptionists.FirstOrDefaultAsync(r => r.ApplicationUserId == userId);
            
            if (receptionist == null) return NotFound();

            var today = DateTime.Today;
            
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d.ApplicationUser)
                .Include(a => a.Patient)
                .ThenInclude(p => p.ApplicationUser)
                .Include(a => a.Invoice)
                .Where(a => a.AppointmentDateTime.Date == today)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            var pendingInvoicesCount = await _context.Invoices
                .CountAsync(i => i.Status == PaymentStatus.Pending);

            var vm = new ReceptionistDashboardViewModel
            {
                CashDrawerBalance = receptionist.CashDrawerBalance,
                PendingInvoicesCount = pendingInvoicesCount,
                TodayAppointmentsCount = appointments.Count,
                UpcomingAppointments = appointments
            };

            return View(vm);
        }

        // Book Appointment
        [HttpGet]
        public IActionResult BookAppointment()
        {
            var doctors = _context.Set<Doctor>().Include(d => d.ApplicationUser).Include(d => d.Specialization).ToList();
            var docList = doctors.Select(d => new { Id = d.Id, Name = $"Dr. {d.ApplicationUser?.FullName} - {d.Specialization?.Name}" }).ToList();
            ViewBag.Doctors = new SelectList(docList, "Id", "Name");
            return View(new BookAppointmentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var doctors = _context.Set<Doctor>().Include(d => d.ApplicationUser).Include(d => d.Specialization).ToList();
                var docList = doctors.Select(d => new { Id = d.Id, Name = $"Dr. {d.ApplicationUser?.FullName} - {d.Specialization?.Name}" }).ToList();
                ViewBag.Doctors = new SelectList(docList, "Id", "Name");
                return View(model);
            }

            var patient = await _context.Set<Patient>().Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.CNIC == model.CNIC);
            
            if (patient == null)
            {
                // Register patient on the fly
                if (string.IsNullOrEmpty(model.NewPatientEmail) || string.IsNullOrEmpty(model.NewPatientFullName) || string.IsNullOrEmpty(model.NewPatientPassword))
                {
                    ModelState.AddModelError("", "CNIC not found. Please provide Full Name, Email, DOB, Phone, and Password to register the patient.");
                    var doctors = _context.Set<Doctor>().Include(d => d.ApplicationUser).Include(d => d.Specialization).ToList();
                    var docList = doctors.Select(d => new { Id = d.Id, Name = $"Dr. {d.ApplicationUser?.FullName} - {d.Specialization?.Name}" }).ToList();
                    ViewBag.Doctors = new SelectList(docList, "Id", "Name");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.NewPatientEmail,
                    Email = model.NewPatientEmail,
                    FullName = model.NewPatientFullName,
                    PhoneNumber = model.NewPatientPhone,
                    EmailConfirmed = true
                };

                // Use the provided password
                var result = await _userManager.CreateAsync(user, model.NewPatientPassword);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Failed to create patient account. Email may already be in use.");
                    var doctors = _context.Set<Doctor>().Include(d => d.ApplicationUser).Include(d => d.Specialization).ToList();
                    var docList = doctors.Select(d => new { Id = d.Id, Name = $"Dr. {d.ApplicationUser?.FullName} - {d.Specialization?.Name}" }).ToList();
                    ViewBag.Doctors = new SelectList(docList, "Id", "Name");
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, "Patient");

                patient = new Patient
                {
                    ApplicationUserId = user.Id,
                    CNIC = model.CNIC,
                    DateOfBirth = model.NewPatientDOB ?? DateTime.Today.AddYears(-20)
                };
                
                _context.Set<Patient>().Add(patient);
                await _context.SaveChangesAsync();
            }

            // Create the appointment
            var doctor = await _context.Set<Doctor>().FindAsync(model.DoctorId);
            if (doctor == null) return NotFound();

            var appointment = new Appointment
            {
                DoctorId = model.DoctorId,
                PatientId = patient.Id,
                AppointmentDateTime = model.AppointmentDateTime,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Create Invoice
            var invoice = new Invoice
            {
                AppointmentId = appointment.Id,
                Amount = doctor.ConsultationFee,
                Status = PaymentStatus.Pending,
                IssuedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Appointment booked successfully for {patient.ApplicationUser?.FullName}. Please collect the payment to confirm the appointment.";
            // Redirect to Invoice details (Assuming InvoicesController exists)
            return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectPayment(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var receptionist = await _context.Receptionists.FirstOrDefaultAsync(r => r.ApplicationUserId == userId);
            
            if (receptionist == null) return NotFound();

            if (invoice.Status != PaymentStatus.Paid)
            {
                invoice.Status = PaymentStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;
                invoice.PaymentMethod = "Cash at Desk";
                invoice.CollectedByReceptionistId = receptionist.Id;

                receptionist.CashDrawerBalance += invoice.Amount;
                
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Collected Rs. {invoice.Amount:N0} successfully.";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> MyDrawer()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var receptionist = await _context.Receptionists
                .Include(r => r.CashHandovers)
                .ThenInclude(h => h.AdminUser)
                .FirstOrDefaultAsync(r => r.ApplicationUserId == userId);

            if (receptionist == null) return NotFound();

            var pendingInvoices = await _context.Invoices
                .Include(i => i.Appointment)
                .ThenInclude(a => a.Doctor)
                .ThenInclude(d => d.ApplicationUser)
                .Where(i => i.CollectedByReceptionistId == receptionist.Id 
                         && i.Status == PaymentStatus.Paid 
                         && !i.IsHandedOverToDoctor)
                .ToListAsync();

            var groups = pendingInvoices.GroupBy(i => i.Appointment.Doctor)
                .Select(g => new DoctorCashGroup
                {
                    DoctorId = g.Key.Id,
                    DoctorName = "Dr. " + g.Key.ApplicationUser.FullName,
                    TotalCollected = g.Sum(i => i.Amount)
                }).ToList();

            var vm = new MyDrawerViewModel
            {
                TotalDrawerBalance = receptionist.CashDrawerBalance,
                DoctorGroups = groups
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandoverCashToDoctor(int doctorId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var receptionist = await _context.Receptionists.FirstOrDefaultAsync(r => r.ApplicationUserId == userId);
            
            if (receptionist == null) return NotFound();

            var pendingInvoices = await _context.Invoices
                .Include(i => i.Appointment)
                .Where(i => i.CollectedByReceptionistId == receptionist.Id 
                         && i.Status == PaymentStatus.Paid 
                         && !i.IsHandedOverToDoctor
                         && i.Appointment.DoctorId == doctorId)
                .ToListAsync();

            if (!pendingInvoices.Any())
            {
                TempData["Error"] = "No pending cash to hand over to this doctor.";
                return RedirectToAction(nameof(MyDrawer));
            }

            decimal totalAmount = pendingInvoices.Sum(i => i.Amount);
            
            foreach(var invoice in pendingInvoices)
            {
                invoice.IsHandedOverToDoctor = true;
            }

            receptionist.CashDrawerBalance -= totalAmount;
            if (receptionist.CashDrawerBalance < 0) receptionist.CashDrawerBalance = 0; // sanity check

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Successfully handed over Rs. {totalAmount:N0} to the doctor.";

            return RedirectToAction(nameof(MyDrawer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandoverCash()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var receptionist = await _context.Receptionists.FirstOrDefaultAsync(r => r.ApplicationUserId == userId);
            
            if (receptionist == null) return NotFound();

            if (receptionist.CashDrawerBalance <= 0)
            {
                TempData["Error"] = "Drawer is empty.";
                return RedirectToAction(nameof(MyDrawer));
            }

            // Find an admin user to assign as receiver (just picking the first admin for demo purposes, 
            // in a real app the admin might have to confirm receipt)
            var adminUser = await _userManager.GetUsersInRoleAsync("Admin");
            var receiver = adminUser.FirstOrDefault();

            if (receiver != null)
            {
                var handover = new CashHandover
                {
                    ReceptionistId = receptionist.Id,
                    Amount = receptionist.CashDrawerBalance,
                    HandoverDate = DateTime.UtcNow,
                    AdminUserId = receiver.Id
                };

                _context.CashHandovers.Add(handover);
                
                // Reset Drawer
                receptionist.CashDrawerBalance = 0;
                
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Handed over Rs. {handover.Amount:N0} to Admin.";
            }

            return RedirectToAction(nameof(MyDrawer));
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientByCNIC(string cnic)
        {
            var patient = await _context.Set<Patient>()
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.CNIC == cnic);

            if (patient != null && patient.ApplicationUser != null)
            {
                return Json(new { 
                    found = true, 
                    fullName = patient.ApplicationUser.FullName, 
                    email = patient.ApplicationUser.Email,
                    phone = patient.PhoneNumber ?? "",
                    dob = patient.DateOfBirth?.ToString("yyyy-MM-dd") ?? ""
                });
            }
            return Json(new { found = false });
        }
    }
}
