using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCareAppointmentSystem.Data;
using Microsoft.AspNetCore.Identity;
using HealthCareAppointmentSystem.Models;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditLogsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs.OrderByDescending(l => l.Timestamp).Take(100).ToListAsync();
            var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);

            ViewBag.Users = users;
            return View(logs);
        }
    }
}
