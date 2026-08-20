using HealthCareAppointmentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareAppointmentSystem.ViewModels;
using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Patients
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.Patients
                .Include(p => p.ApplicationUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => 
                    p.ApplicationUser!.FullName.Contains(searchString) ||
                    p.ApplicationUser!.Email!.Contains(searchString) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(searchString)));
            }

            var patients = await query.ToListAsync();
            return View(patients);
        }

        // GET: /Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.ApplicationUser)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                    .ThenInclude(d => d!.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient is null) return NotFound();
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                try
                {
                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Patient profile deleted successfully.";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Cannot delete this patient because they have associated records (e.g. appointments).";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Patients/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(PatientCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Patient");

                    var patient = new Patient
                    {
                        ApplicationUserId = user.Id,
                        DateOfBirth = model.DateOfBirth,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address
                    };

                    _context.Add(patient);
                    await _context.SaveChangesAsync();
                    
                    TempData["Message"] = $"Patient account created successfully. They can log in with Email: {model.Email}.";
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            return View(model);
        }
        // most real systems separate "who can create an account" from "who can manage records."
    }
}
