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
            ViewBag.Doctors = new SelectList(_context.Set<Doctor>().Include(d => d.ApplicationUser).ToList(), "Id", "ApplicationUser.FullName");
            return View(new BookAppointmentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Doctors = new SelectList(_context.Set<Doctor>().Include(d => d.ApplicationUser).ToList(), "Id", "ApplicationUser.FullName");
                return View(model);
            }

            var patient = await _context.Set<Patient>().Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.CNIC == model.CNIC);
            
            if (patient == null)
            {
                // Register patient on the fly
                if (string.IsNullOrEmpty(model.NewPatientEmail) || string.IsNullOrEmpty(model.NewPatientFullName))
                {
                    ModelState.AddModelError("", "CNIC not found. Please provide Full Name, Email, DOB, Gender and Phone to register the patient.");
                    ViewBag.Doctors = new SelectList(_context.Set<Doctor>().Include(d => d.ApplicationUser).ToList(), "Id", "ApplicationUser.FullName");
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

                // Generate a random password for them
                var result = await _userManager.CreateAsync(user, "Patient@123");
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Failed to create patient account. Email may already be in use.");
                    ViewBag.Doctors = new SelectList(_context.Set<Doctor>().Include(d => d.ApplicationUser).ToList(), "Id", "ApplicationUser.FullName");
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

            TempData["Success"] = $"Appointment booked successfully for {patient.ApplicationUser?.FullName}.";
            return RedirectToAction(nameof(Dashboard));
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

            return View(receptionist);
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
    }
}
