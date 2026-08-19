using System.ComponentModel.DataAnnotations;
using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public SelectList Specializations { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [Display(Name = "I am a...")]
            public string Role { get; set; }

            // --- Patient Fields ---
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime? DateOfBirth { get; set; }

            [Phone]
            [Display(Name = "Phone Number")]
            public string? PhoneNumber { get; set; }

            public string? Address { get; set; }

            // --- Doctor Fields ---
            [Display(Name = "Specialization")]
            public int? SpecializationId { get; set; }

            [Display(Name = "License Number")]
            public string? LicenseNumber { get; set; }

            [Display(Name = "Years of Experience")]
            public int? YearsOfExperience { get; set; }

            [Display(Name = "Consultation Fee")]
            public decimal? ConsultationFee { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Dashboard");

            // Re-populate dropdown just in case we return to the page
            Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");

            // Additional validation for Doctor fields
            if (Input.Role == "Doctor")
            {
                if (!Input.SpecializationId.HasValue) ModelState.AddModelError("Input.SpecializationId", "Specialization is required for doctors.");
                if (string.IsNullOrWhiteSpace(Input.LicenseNumber)) ModelState.AddModelError("Input.LicenseNumber", "License Number is required for doctors.");
                if (!Input.YearsOfExperience.HasValue) ModelState.AddModelError("Input.YearsOfExperience", "Years of Experience is required for doctors.");
                if (!Input.ConsultationFee.HasValue) ModelState.AddModelError("Input.ConsultationFee", "Consultation Fee is required for doctors.");
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.FullName = Input.FullName;
                user.Email = Input.Email;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // Assign Role
                    await _userManager.AddToRoleAsync(user, Input.Role);

                    // Create Profile
                    if (Input.Role == "Patient")
                    {
                        var patient = new Patient
                        {
                            ApplicationUserId = user.Id,
                            DateOfBirth = Input.DateOfBirth,
                            PhoneNumber = Input.PhoneNumber,
                            Address = Input.Address
                        };
                        _context.Patients.Add(patient);
                        await _context.SaveChangesAsync();
                    }
                    else if (Input.Role == "Doctor")
                    {
                        var doctor = new Doctor
                        {
                            ApplicationUserId = user.Id,
                            SpecializationId = Input.SpecializationId.Value,
                            LicenseNumber = Input.LicenseNumber!,
                            YearsOfExperience = Input.YearsOfExperience.Value,
                            ConsultationFee = Input.ConsultationFee.Value
                        };
                        _context.Doctors.Add(doctor);
                        await _context.SaveChangesAsync();
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'.");
            }
        }
    }
}
