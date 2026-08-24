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

        // GET: Accounts/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new AccountCreateViewModel());
        }

        // POST: Accounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

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
                await _userManager.AddToRoleAsync(user, model.Role);

                if (model.Role == "Receptionist")
                {
                    var context = HttpContext.RequestServices.GetRequiredService<HealthCareAppointmentSystem.Data.ApplicationDbContext>();
                    context.Receptionists.Add(new Receptionist { ApplicationUserId = user.Id });
                    await context.SaveChangesAsync();
                }

                TempData["Message"] = $"Account for {user.Email} created successfully with role '{model.Role}'.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
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

        // GET: Accounts/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            
            var vm = new AccountViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Roles = string.Join(", ", roles),
                IsBanned = await _userManager.IsLockedOutAsync(user)
            };

            return View(vm);
        }

        // GET: Accounts/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new AccountEditViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty
            };

            return View(vm);
        }

        // POST: Accounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AccountEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();

                user.Email = model.Email;
                user.UserName = model.Email;
                user.FullName = model.FullName;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                        if (!passResult.Succeeded)
                        {
                            foreach (var error in passResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            return View(model);
                        }
                    }

                    TempData["Message"] = "Account updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
    }
}
