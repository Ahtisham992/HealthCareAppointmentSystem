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
