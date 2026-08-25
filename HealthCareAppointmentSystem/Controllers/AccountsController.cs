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
        private readonly HealthCareAppointmentSystem.Data.ApplicationDbContext _context;

        public AccountsController(UserManager<ApplicationUser> userManager, HealthCareAppointmentSystem.Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Accounts/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var context = HttpContext.RequestServices.GetRequiredService<HealthCareAppointmentSystem.Data.ApplicationDbContext>();
            ViewBag.Specializations = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await context.Specializations.ToListAsync(), "Id", "Name");
            return View(new AccountCreateViewModel());
        }

        // POST: Accounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateViewModel model)
        {
            if (model.Role == "Doctor" || model.Role == "Patient")
            {
                if (string.IsNullOrWhiteSpace(model.CNIC))
                {
                    ModelState.AddModelError("CNIC", "CNIC is required for Doctors and Patients.");
                }
                else
                {
                    var context = HttpContext.RequestServices.GetRequiredService<HealthCareAppointmentSystem.Data.ApplicationDbContext>();
                    bool cnicExists = await context.Patients.AnyAsync(p => p.CNIC == model.CNIC) || 
                                      await context.Doctors.AnyAsync(d => d.CNIC == model.CNIC);
                    if (cnicExists)
                    {
                        ModelState.AddModelError("CNIC", "An account with this CNIC already exists.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var context = HttpContext.RequestServices.GetRequiredService<HealthCareAppointmentSystem.Data.ApplicationDbContext>();
                ViewBag.Specializations = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await context.Specializations.ToListAsync(), "Id", "Name");
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
                else if (model.Role == "Doctor")
                {
                    var context = HttpContext.RequestServices.GetRequiredService<HealthCareAppointmentSystem.Data.ApplicationDbContext>();
                    var defaultSpecialization = await context.Specializations.FirstOrDefaultAsync();
                    
                    context.Doctors.Add(new Doctor 
                    { 
                        ApplicationUserId = user.Id,
                        CNIC = model.CNIC!,
                        SpecializationId = model.SpecializationId ?? defaultSpecialization?.Id ?? 1,
                        LicenseNumber = string.IsNullOrWhiteSpace(model.LicenseNumber) ? "PENDING-UPDATE-REQUIRED" : model.LicenseNumber,
                        YearsOfExperience = model.YearsOfExperience ?? 0,
                        ConsultationFee = model.ConsultationFee ?? 0m,
                        IsApproved = true // Admin creates them, so they are approved by default
                    });
                    await context.SaveChangesAsync();
                }
                else if (model.Role == "Patient")
                {
                    var context = HttpContext.RequestServices.GetRequiredService<HealthCareAppointmentSystem.Data.ApplicationDbContext>();
                    
                    context.Patients.Add(new Patient 
                    { 
                        ApplicationUserId = user.Id,
                        CNIC = model.CNIC!,
                        PhoneNumber = model.PhoneNumber,
                        DateOfBirth = model.DateOfBirth,
                        Address = model.Address
                    });
                    await context.SaveChangesAsync();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = currentUser?.Id ?? user.Id,
                    Action = $"Created {model.Role} account for {user.Email}"
                });
                await _context.SaveChangesAsync();

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
                
                var currentUser = await _userManager.GetUserAsync(User);
                string actionMsg = "";
                
                if (isBanned)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    actionMsg = $"Unbanned user {user.Email}";
                    TempData["Message"] = $"User {user.Email} has been unbanned.";
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    actionMsg = $"Banned user {user.Email}";
                    TempData["Message"] = $"User {user.Email} has been banned.";
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = currentUser?.Id ?? user.Id,
                    Action = actionMsg
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

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
                        _context.AuditLogs.Add(new AuditLog
                        {
                            UserId = currentUser?.Id ?? user.Id,
                            Action = $"Deleted user account {user.Email}"
                        });
                        await _context.SaveChangesAsync();
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

                    var currentUser = await _userManager.GetUserAsync(User);
                    _context.AuditLogs.Add(new AuditLog
                    {
                        UserId = currentUser?.Id ?? user.Id,
                        Action = $"Admin updated account details for {user.Email}"
                    });
                    await _context.SaveChangesAsync();

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
