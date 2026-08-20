using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using HealthCareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Doctors
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Specialization)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d => 
                    d.ApplicationUser!.FullName.Contains(searchString) || 
                    d.ApplicationUser!.Email!.Contains(searchString) || 
                    d.Specialization!.Name.Contains(searchString) ||
                    d.LicenseNumber.Contains(searchString));
            }

            var doctors = await query.ToListAsync();
            return View(doctors);
        }

        // GET: /Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.ApplicationUser)
                .Include(d => d.Specialization)
                .Include(d => d.Appointments).ThenInclude(a => a.Invoice)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor is null) return NotFound();

            var reviews = await _context.Reviews
                .Include(r => r.Patient).ThenInclude(p => p.ApplicationUser)
                .Where(r => r.DoctorId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            var totalEarnings = doctor.Appointments
                .Where(a => a.Invoice != null && (a.Invoice.Status == PaymentStatus.Paid || a.Invoice.Status == PaymentStatus.Refunded))
                .Sum(a => a.Invoice!.Amount - (a.Invoice.RefundAmount ?? 0));
                
            ViewBag.Reviews = reviews;
            ViewBag.TotalEarnings = totalEarnings;

            return View(doctor);
        }

        // GET: /Doctors/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: /Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(DoctorCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true // Auto confirm since admin is creating it
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");

                    var doctor = new Doctor
                    {
                        ApplicationUserId = user.Id,
                        SpecializationId = model.SpecializationId,
                        LicenseNumber = model.LicenseNumber,
                        YearsOfExperience = model.YearsOfExperience,
                        ConsultationFee = model.ConsultationFee,
                        IsApproved = true // Admin creates it, so it can be auto-approved or set to true
                    };

                    _context.Add(doctor);
                    await _context.SaveChangesAsync();
                    
                    TempData["Message"] = $"Doctor account created successfully. They can now log in with Email: {model.Email} and the password you provided.";
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");
            return View(model);
        }

        // GET: /Doctors/Edit/5
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        }
    }
}
