using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AccountsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountsController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var users = await _userManager.Users.ToListAsync();
            var model = new List<AccountViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var rolesStr = string.Join(", ", roles);

                if (!string.IsNullOrEmpty(searchString))
                {
                    bool match = (user.FullName != null && user.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                                 (user.Email != null && user.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                                 rolesStr.Contains(searchString, StringComparison.OrdinalIgnoreCase);
                    if (!match) continue;
                }

                model.Add(new AccountViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Roles = rolesStr,
                    IsBanned = await _userManager.IsLockedOutAsync(user)
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBan(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var isBanned = await _userManager.IsLockedOutAsync(user);
                await _userManager.SetLockoutEnabledAsync(user, true);
                if (isBanned)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    TempData["Message"] = $"User {user.Email} has been unbanned.";
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    TempData["Message"] = $"User {user.Email} has been banned.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                try
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        TempData["Error"] = $"Cannot delete user {user.Email}. They may have associated records (like appointments). Please ban them instead.";
                    }
                    else
                    {
                        TempData["Message"] = $"User {user.Email} deleted successfully.";
                    }
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = $"Cannot delete user {user.Email} because they have associated records (e.g. appointments or profile). Please ban them instead.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
