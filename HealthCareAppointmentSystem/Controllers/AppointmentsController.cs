using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize] // any logged-in user (Admin, Doctor, or Patient) can access - visibility is filtered per role below
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Appointments
        // Admin sees all appointments. Doctor sees only their own. Patient sees only their own.
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is null) return Challenge();

            IQueryable<Appointment> query = _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.ApplicationUser)
                .Include(a => a.Doctor).ThenInclude(d => d!.Specialization)
                .Include(a => a.Patient).ThenInclude(p => p!.ApplicationUser)
                .OrderByDescending(a => a.AppointmentDateTime);

            if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == currentUser.Id);
                query = query.Where(a => a.DoctorId == (doctor != null ? doctor.Id : -1));
            }
            else if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id);
                query = query.Where(a => a.PatientId == (patient != null ? patient.Id : -1));
            }
            // Admin: no filter, sees everything.

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(a => 
                    (a.Doctor!.ApplicationUser!.FullName.Contains(searchString)) ||
                    (a.Doctor!.ApplicationUser!.Email!.Contains(searchString)) ||
                    (a.Patient!.ApplicationUser!.FullName.Contains(searchString)) ||
                    (a.Patient!.ApplicationUser!.Email!.Contains(searchString)) ||
                    a.Status.ToString().Contains(searchString));
            }

            return View(await query.ToListAsync());
        }

        // GET: /Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.ApplicationUser)
                .Include(a => a.Patient).ThenInclude(p => p!.ApplicationUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment is null) return NotFound();

            return View(appointment);
        }

        // GET: /Appointments/Create
        // Patients book for themselves. Admins can book on behalf of any patient.
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> Create(int? doctorId)
        {
            var vm = new AppointmentViewModel
            {
                DoctorId = doctorId ?? 0,
                AvailableDoctors = await _context.Doctors.Include(d => d.ApplicationUser).Include(d => d.Specialization).Where(d => d.IsApproved).ToListAsync(),
                AvailablePatients = User.IsInRole("Admin")
                    ? await _context.Patients.Include(p => p.ApplicationUser).ToListAsync()
                    : new List<Patient>()
            };
            return View(vm);
        }

        // POST: /Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> Create(AppointmentViewModel vm)
        {
            int patientId;

            if (User.IsInRole("Patient"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser!.Id);
                if (patient is null)
                {
                    ModelState.AddModelError(string.Empty, "No patient profile found for this account.");
                    patientId = 0;
                }
                else
                {
                    patientId = patient.Id;
                }
            }
            else
            {
                // Admin must have selected a patient
                if (vm.PatientId is null)
                {
                    ModelState.AddModelError(nameof(vm.PatientId), "Please select a patient.");
                }
                patientId = vm.PatientId ?? 0;
            }

            if (ModelState.IsValid)
            {
                var appointment = new Appointment
                {
                    DoctorId = vm.DoctorId,
                    PatientId = patientId,
                    AppointmentDateTime = vm.AppointmentDateTime,
                    Notes = vm.Notes,
                    Status = AppointmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdowns if validation failed
            vm.AvailableDoctors = await _context.Doctors.Include(d => d.ApplicationUser).Include(d => d.Specialization).Where(d => d.IsApproved).ToListAsync();
            vm.AvailablePatients = User.IsInRole("Admin")
                ? await _context.Patients.Include(p => p.ApplicationUser).ToListAsync()
                : new List<Patient>();
            return View(vm);
        }

        // GET: /Appointments/Edit/5
        // Doctors and Admins can update status and refund. Patients use RequestCancellation.
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment is null) return NotFound();

            if (!await UserCanModifyAppointment(appointment)) return Forbid();

            return View(appointment);
        }

        // POST: /Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DoctorId,PatientId,AppointmentDateTime,Status,IsRefunded,Notes,CreatedAt,CancellationReason")] Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();

            if (!await UserCanModifyAppointment(appointment)) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null) 
                    {
                        var auditLog = new AuditLog
                        {
                            UserId = currentUser.Id,
                            Action = "Edited Appointment",
                            Details = $"Appointment ID: {appointment.Id}, New Status: {appointment.Status}" + (appointment.Status == AppointmentStatus.Cancelled ? $", Reason: {appointment.CancellationReason}" : "")
                        };
                        _context.AuditLogs.Add(auditLog);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(e => e.Id == appointment.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        // GET: /Appointments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d!.ApplicationUser)
                .Include(a => a.Patient).ThenInclude(p => p!.ApplicationUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment is null) return NotFound();

            return View(appointment);
        }

        // POST: /Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor,Patient,Admin")]
        public async Task<IActionResult> MarkCompleted(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Validate permissions
            if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == currentUser.Id);
                if (appointment.DoctorId != doctor?.Id) return Forbid();
            }
            else if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id);
                if (appointment.PatientId != patient?.Id) return Forbid();
            }

            if (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
            {
                appointment.Status = AppointmentStatus.Completed;
                
                var auditLog = new AuditLog
                {
                    UserId = currentUser.Id,
                    Action = "Marked Appointment Completed",
                    Details = $"Appointment ID: {appointment.Id}"
                };
                _context.AuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        /// <summary>
        /// Authorization rule: Admins can modify anything. Doctors can only modify
        /// appointments assigned to them (typically to change status). Patients can
        /// only modify their own appointments.
        /// </summary>
        private async Task<bool> UserCanModifyAppointment(Appointment appointment)
        {
            if (User.IsInRole("Admin")) return true;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is null) return false;

            if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == currentUser.Id);
                return doctor != null && doctor.Id == appointment.DoctorId;
            }

            if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id);
                return patient != null && patient.Id == appointment.PatientId;
            }

            return false;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> RequestCancellation(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (!await UserCanModifyAppointment(appointment)) return Forbid();

            // Only allow if pending or confirmed
            if (appointment.Status == AppointmentStatus.Pending || appointment.Status == AppointmentStatus.Confirmed)
            {
                appointment.Status = AppointmentStatus.CancellationRequested;
                _context.Update(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var selectedDate))
                return BadRequest("Invalid date format.");

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null) return NotFound("Doctor not found.");

            var bookedAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDateTime.Date == selectedDate.Date && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.AppointmentDateTime.TimeOfDay)
                .ToListAsync();

            var availableSlots = new List<string>();
            var slotDuration = doctor.SlotDurationMinutes > 0 ? doctor.SlotDurationMinutes : 20;

            var currentSlot = doctor.AvailableFrom;
            while (currentSlot.Add(TimeSpan.FromMinutes(slotDuration)) <= doctor.AvailableTo)
            {
                if (!bookedAppointments.Contains(currentSlot))
                {
                    availableSlots.Add(currentSlot.ToString(@"hh\:mm"));
                }
                currentSlot = currentSlot.Add(TimeSpan.FromMinutes(slotDuration));
            }

            return Json(availableSlots);
        }
    }
}
