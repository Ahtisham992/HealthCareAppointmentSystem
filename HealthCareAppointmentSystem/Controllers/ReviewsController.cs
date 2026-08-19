using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareAppointmentSystem.Data;
using Microsoft.AspNetCore.Identity;
using HealthCareAppointmentSystem.Models;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reviews/Create?appointmentId=5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Create(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.ApplicationUser)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || appointment.Patient?.ApplicationUserId != currentUser.Id)
                return Forbid();

            // Check if review already exists
            var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (existingReview != null)
            {
                TempData["Message"] = "You have already reviewed this appointment.";
                return RedirectToAction("Index", "Appointments");
            }

            var review = new Review
            {
                AppointmentId = appointmentId,
                DoctorId = appointment.DoctorId,
                PatientId = appointment.PatientId
            };

            ViewBag.DoctorName = appointment.Doctor?.ApplicationUser?.FullName;
            return View(review);
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentId,DoctorId,PatientId,Rating,Comment")] Review review)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id);
            if (patient == null || patient.Id != review.PatientId) return Forbid();

            if (ModelState.IsValid)
            {
                _context.Add(review);
                
                var auditLog = new AuditLog
                {
                    UserId = currentUser.Id,
                    Action = "Left Review",
                    Details = $"Rating: {review.Rating} for Doctor ID: {review.DoctorId}"
                };
                _context.AuditLogs.Add(auditLog);
                
                await _context.SaveChangesAsync();
                TempData["Message"] = "Thank you for your review!";
                return RedirectToAction("Index", "Appointments");
            }

            var doc = await _context.Doctors.Include(d => d.ApplicationUser).FirstOrDefaultAsync(d => d.Id == review.DoctorId);
            ViewBag.DoctorName = doc?.ApplicationUser?.FullName;
            return View(review);
        }
    }
}
