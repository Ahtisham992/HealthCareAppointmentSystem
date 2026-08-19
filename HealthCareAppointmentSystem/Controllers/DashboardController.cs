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
            var appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p!.ApplicationUser)
                .Where(a => a.DoctorId == doctor.Id)
                .ToListAsync();

            var now = DateTime.Now;
            var todayAppointments = appointments.Count(a => a.AppointmentDateTime.Date == today.Date);
            var upcomingAppointments = appointments.Where(a => a.AppointmentDateTime >= today && a.Status != AppointmentStatus.Cancelled).ToList();
            
            var pendingConfirmations = appointments.Count(a => a.Status == AppointmentStatus.Pending && a.AppointmentDateTime > now);
            var pendingCompletions = appointments.Count(a => (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) && a.AppointmentDateTime.AddMinutes(doctor.SlotDurationMinutes > 0 ? doctor.SlotDurationMinutes : 20) <= now);

            var vm = new DoctorDashboardViewModel
            {
                DoctorProfile = doctor,
                TodayAppointmentsCount = todayAppointments,
                UpcomingAppointmentsCount = upcomingAppointments.Count,
                PendingConfirmationsCount = pendingConfirmations,
                PendingCompletionsCount = pendingCompletions,
                UpcomingAppointments = upcomingAppointments.OrderBy(a => a.AppointmentDateTime).Take(10).ToList()
            };

            if (string.IsNullOrWhiteSpace(doctor.Education) || string.IsNullOrWhiteSpace(doctor.ProfilePictureUrl))
            {
                ViewBag.NeedsProfileCompletion = true;
            }

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

            var currentUser = await _userManager.GetUserAsync(User);
            var patient = currentUser != null ? await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id) : null;
            int pendingReviews = 0;
            if (patient != null)
            {
                var now = DateTime.Now;
                pendingReviews = await _context.Appointments
                    .Where(a => a.PatientId == patient.Id && a.Status == AppointmentStatus.Completed)
                    .GroupJoin(_context.Reviews, a => a.Id, r => r.AppointmentId, (a, r) => new { a, r })
                    .SelectMany(x => x.r.DefaultIfEmpty(), (x, r) => new { x.a, r })
                    .CountAsync(x => x.r == null);
            }

            var vm = new PatientDashboardViewModel
            {
                SearchTerm = searchTerm ?? string.Empty,
                SpecializationId = specializationId,
                AvailableDoctors = await query.ToListAsync(),
                PendingReviewsCount = pendingReviews
            };

            ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name", specializationId);

            return View(vm);
        }
    }
}
