using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin")) return RedirectToAction(nameof(AdminDashboard));
            if (User.IsInRole("Doctor")) return RedirectToAction(nameof(DoctorDashboard));
            if (User.IsInRole("Patient")) return RedirectToAction(nameof(PatientDashboard));

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalDoctors = await _context.Doctors.CountAsync(),
                TotalPatients = await _context.Patients.CountAsync(),
                PendingApprovals = await _context.Doctors.CountAsync(d => !d.IsApproved),
                TotalAppointments = await _context.Appointments.CountAsync()
            };
            return View(vm);
        }

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> DoctorDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var doctor = await _context.Doctors
                .Include(d => d.Specialization)
                .FirstOrDefaultAsync(d => d.ApplicationUserId == user!.Id);

            if (doctor == null) return NotFound();

            var today = DateTime.UtcNow.Date;
            var upcoming = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p!.ApplicationUser)
                .Where(a => a.DoctorId == doctor.Id && a.AppointmentDateTime >= today)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            var vm = new DoctorDashboardViewModel
            {
                DoctorProfile = doctor,
                TodayAppointmentsCount = upcoming.Count(a => a.AppointmentDateTime.Date == today),
                UpcomingAppointmentsCount = upcoming.Count,
                UpcomingAppointments = upcoming.Take(5).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> PatientDashboard(string? searchTerm, int? specializationId)
        {
            var query = _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Specialization)
                .Where(d => d.IsApproved)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.ApplicationUser!.FullName.Contains(searchTerm));
            }

            if (specializationId.HasValue)
            {
                query = query.Where(d => d.SpecializationId == specializationId.Value);
            }

            var vm = new PatientDashboardViewModel
            {
                SearchTerm = searchTerm ?? string.Empty,
                SpecializationId = specializationId,
                AvailableDoctors = await query.ToListAsync()
            };

            ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name", specializationId);

            return View(vm);
        }
    }
}
