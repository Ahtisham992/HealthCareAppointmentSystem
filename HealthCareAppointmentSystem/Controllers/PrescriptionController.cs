using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class PrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrescriptionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Create(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == user!.Id);

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .ThenInclude(p => p!.ApplicationUser)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctor!.Id);

            if (appointment == null || appointment.Status != AppointmentStatus.Completed)
            {
                TempData["Error"] = "Appointment not found or not yet completed.";
                return RedirectToAction("Index", "Appointments");
            }

            // Check if prescription already exists
            var existing = await _context.Prescriptions.FirstOrDefaultAsync(p => p.AppointmentId == appointment.Id);
            if (existing != null)
            {
                TempData["Message"] = "A prescription has already been created for this appointment.";
                return RedirectToAction("Index", "Appointments");
            }

            var vm = new CreatePrescriptionViewModel
            {
                AppointmentId = appointment.Id,
                PatientName = appointment.Patient?.ApplicationUser?.FullName,
                AppointmentDate = appointment.AppointmentDateTime
            };
            
            // Start with one empty row
            vm.Items.Add(new PrescriptionItemViewModel());

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Create(CreatePrescriptionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == user!.Id);

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == model.AppointmentId && a.DoctorId == doctor!.Id);

            if (appointment == null || appointment.Status != AppointmentStatus.Completed)
            {
                TempData["Error"] = "Appointment not found or not yet completed.";
                return RedirectToAction("Index", "Appointments");
            }

            if (ModelState.IsValid)
            {
                var prescription = new Prescription
                {
                    AppointmentId = appointment.Id,
                    DoctorNotes = model.DoctorNotes,
                    Status = PrescriptionStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var item in model.Items)
                {
                    if (!string.IsNullOrWhiteSpace(item.MedicineName))
                    {
                        prescription.Items.Add(new PrescriptionItem
                        {
                            MedicineName = item.MedicineName,
                            Dosage = item.Dosage,
                            Frequency = item.Frequency,
                            Duration = item.Duration,
                            SpecialInstructions = item.SpecialInstructions
                        });
                    }
                }

                if (prescription.Items.Count == 0)
                {
                    ModelState.AddModelError("", "At least one medicine is required.");
                    return View(model);
                }

                _context.Prescriptions.Add(prescription);
                
                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user!.Email,
                    Action = "Prescription Created",
                    Details = $"Created prescription for Appointment ID: {appointment.Id} with {prescription.Items.Count} items."
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = "Prescription sent to pharmacy successfully!";
                return RedirectToAction("Index", "Appointments");
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,Pharmacist,Patient")]
        public async Task<IActionResult> Print(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.ApplicationUser)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.Specialization)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a!.Patient)
                        .ThenInclude(pat => pat!.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
            {
                TempData["Error"] = "Prescription not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(prescription);
        }
    }
}
