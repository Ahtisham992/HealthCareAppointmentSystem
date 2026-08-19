using HealthCareAppointmentSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Patients
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .Include(p => p.ApplicationUser)
                .ToListAsync();
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

        // Note: Patient profile creation happens through the normal Register flow
        // (a new user selects "Patient" during registration, which triggers profile
        // creation - see Areas/Identity if you extend registration). Admin here is
        // limited to viewing patients and their appointment history, which mirrors how
        // most real systems separate "who can create an account" from "who can manage records."
    }
}
