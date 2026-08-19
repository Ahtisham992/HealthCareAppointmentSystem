using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Doctors
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Specialization)
                .ToListAsync();
            return View(doctors);
        }

        // GET: /Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Specialization)
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor is null) return NotFound();
            return View(doctor);
        }

        // GET: /Doctors/Create
        // Note: in this scope, Create assumes an ApplicationUser account already exists
        // (created via the normal Register page and then assigned the "Doctor" role by an Admin).
        // A production version would combine account creation + profile creation in one step.
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: /Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApplicationUserId,SpecializationId,LicenseNumber,YearsOfExperience,ConsultationFee")] Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(doctor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdowns();
            return View(doctor);
        }

        // GET: /Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor is null) return NotFound();

            await PopulateDropdowns();
            return View(doctor);
        }

        // POST: /Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ApplicationUserId,SpecializationId,LicenseNumber,YearsOfExperience,ConsultationFee")] Doctor doctor)
        {
            if (id != doctor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Doctors.Any(e => e.Id == doctor.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdowns();
            return View(doctor);
        }

        // GET: /Doctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Specialization)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor is null) return NotFound();
            return View(doctor);
        }

        // POST: /Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctors/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                doctor.IsApproved = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");

            // Only users currently in the "Doctor" role without an existing Doctor profile
            var doctorUserIds = await _context.Doctors.Select(d => d.ApplicationUserId).ToListAsync();
            var allUsers = await _context.Users.ToListAsync();
            var availableUsers = allUsers.Where(u => !doctorUserIds.Contains(u.Id)).Select(u => new
            {
                u.Id,
                DisplayName = string.IsNullOrWhiteSpace(u.FullName) ? u.Email : $"{u.FullName} ({u.Email})"
            });
            ViewBag.Users = new SelectList(availableUsers, "Id", "DisplayName");
        }
    }
}
