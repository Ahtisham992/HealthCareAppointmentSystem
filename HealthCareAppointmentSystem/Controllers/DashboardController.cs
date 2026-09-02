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
            if (User.IsInRole("Receptionist")) return RedirectToAction("Dashboard", "Receptionist");
            if (User.IsInRole("Pharmacist")) return RedirectToAction("Index", "Pharmacist");
            if (User.IsInRole("Accountant")) return RedirectToAction("Dashboard", "Accountant");
            if (User.IsInRole("LabTechnician")) return RedirectToAction("Dashboard", "LabTechnician");

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var doctors = await _context.Doctors.Include(d => d.Specialization).ToListAsync();
            var appointments = await _context.Appointments.ToListAsync();

            // Stats for charts
            var appointmentsByStatus = appointments
                .GroupBy(a => a.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var doctorsBySpecialization = doctors
                .Where(d => d.Specialization != null)
                .GroupBy(d => d.Specialization!.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            var recentLogs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalDoctors = doctors.Count,
                TotalPatients = await _context.Patients.CountAsync(),
                PendingApprovals = doctors.Count(d => !d.IsApproved),
                TotalAppointments = appointments.Count,
                AppointmentsByStatus = appointmentsByStatus,
                DoctorsBySpecialization = doctorsBySpecialization,
                RecentLogs = recentLogs
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

            var now = HealthCareAppointmentSystem.Helpers.TimeHelper.Now;
            var today = now.Date;
            var appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p!.ApplicationUser)
                .Include(a => a.Invoice)
                .Where(a => a.DoctorId == doctor.Id)
                .ToListAsync();

            var todayAppointments = appointments.Count(a => a.AppointmentDateTime.Date == today.Date);
            var upcomingAppointments = appointments.Where(a => a.AppointmentDateTime >= today && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)).ToList();
            
            var pendingConfirmations = appointments.Count(a => a.Status == AppointmentStatus.Pending && a.AppointmentDateTime > now);
            var pendingCompletions = appointments.Count(a => (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) && a.AppointmentDateTime.AddMinutes(doctor.SlotDurationMinutes > 0 ? doctor.SlotDurationMinutes : 20) <= now);

            var totalEarnings = appointments
                .Where(a => a.Invoice != null && (a.Invoice.Status == PaymentStatus.Paid || a.Invoice.Status == PaymentStatus.Refunded))
                .Sum(a => a.Invoice!.Amount - (a.Invoice.RefundAmount ?? 0));

            var vm = new DoctorDashboardViewModel
            {
                DoctorProfile = doctor,
                TodayAppointmentsCount = todayAppointments,
                UpcomingAppointmentsCount = upcomingAppointments.Count,
                PendingConfirmationsCount = pendingConfirmations,
                PendingCompletionsCount = pendingCompletions,
                TotalEarnings = totalEarnings,
                UpcomingAppointments = upcomingAppointments.OrderByDescending(a => a.AppointmentDateTime).Take(10).ToList()
            };

            if (string.IsNullOrWhiteSpace(doctor.Education) || string.IsNullOrWhiteSpace(doctor.ProfilePictureUrl))
            {
                ViewBag.NeedsProfileCompletion = true;
            }

            return View(vm);
        }

        // GET: Dashboard/MyLabResults
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> MyLabResults()
        {
            var user = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == user!.Id);
            
            if (patient == null) return NotFound();

            var labOrders = await _context.LabOrders
                .Include(lo => lo.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.ApplicationUser)
                .Include(lo => lo.LabResult)
                .Where(lo => lo.Appointment!.PatientId == patient.Id)
                .OrderByDescending(lo => lo.OrderedAt)
                .ToListAsync();

            return View(labOrders);
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
            var patient = currentUser != null ? await _context.Patients.Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id) : null;
            
            int pendingReviews = 0;
            int totalAppointments = 0;
            var upcomingAppointments = new List<Appointment>();
            
            if (patient != null)
            {
                var now = HealthCareAppointmentSystem.Helpers.TimeHelper.Now;
                var today = now.Date;
                var allAppointments = await _context.Appointments
                    .Include(a => a.Doctor).ThenInclude(d => d!.ApplicationUser)
                    .Include(a => a.Doctor).ThenInclude(d => d!.Specialization)
                    .Where(a => a.PatientId == patient.Id)
                    .ToListAsync();
                    
                totalAppointments = allAppointments.Count;
                
                upcomingAppointments = allAppointments
                    .Where(a => a.AppointmentDateTime >= today && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .Take(5)
                    .ToList();

                pendingReviews = allAppointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .GroupJoin(_context.Reviews, a => a.Id, r => r.AppointmentId, (a, r) => new { a, r })
                    .SelectMany(x => x.r.DefaultIfEmpty(), (x, r) => new { x.a, r })
                    .Count(x => x.r == null);
            }

            var vm = new PatientDashboardViewModel
            {
                SearchTerm = searchTerm ?? string.Empty,
                SpecializationId = specializationId,
                AvailableDoctors = await query.ToListAsync(),
                PendingReviewsCount = pendingReviews,
                PatientProfile = patient,
                TotalAppointmentsCount = totalAppointments,
                UpcomingAppointmentsCount = upcomingAppointments.Count,
                UpcomingAppointments = upcomingAppointments
            };

            ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name", specializationId);

            return View(vm);
        }
    }
}
