using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using HealthCareAppointmentSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email
            };

            if (User.IsInRole("Patient"))
            {
                vm.Role = "Patient";
                var patient = await _context.Patients.Include(p => p.MedicalProfile).FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
                if (patient != null)
                {
                    vm.DateOfBirth = patient.DateOfBirth;
                    vm.PhoneNumber = patient.PhoneNumber;
                    vm.Address = patient.Address;
                    if (patient.MedicalProfile != null)
                    {
                        vm.BloodGroup = patient.MedicalProfile.BloodGroup;
                        vm.KnownAllergies = patient.MedicalProfile.KnownAllergies;
                        vm.ChronicConditions = patient.MedicalProfile.ChronicConditions;
                    }
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                vm.Role = "Doctor";
                var doctor = await _context.Doctors.Include(d => d.Specialization).FirstOrDefaultAsync(d => d.ApplicationUserId == user.Id);
                if (doctor != null)
                {
                    vm.SpecializationName = doctor.Specialization?.Name;
                    vm.SpecializationId = doctor.SpecializationId;
                    vm.LicenseNumber = doctor.LicenseNumber;
                    vm.YearsOfExperience = doctor.YearsOfExperience;
                    vm.ConsultationFee = doctor.ConsultationFee;
                    vm.IsApproved = doctor.IsApproved;
                    vm.AvailableFrom = doctor.AvailableFrom;
                    vm.AvailableTo = doctor.AvailableTo;
                    vm.SlotDurationMinutes = doctor.SlotDurationMinutes;
                    vm.ProfilePictureUrl = doctor.ProfilePictureUrl;
                    vm.Education = doctor.Education;
                    vm.IsOnLeave = doctor.IsOnLeave;
                }
            }
            else
            {
                vm.Role = "Admin";
            }

            return View(vm);
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email
            };

            if (User.IsInRole("Patient"))
            {
                vm.Role = "Patient";
                var patient = await _context.Patients.Include(p => p.MedicalProfile).FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
                if (patient != null)
                {
                    vm.DateOfBirth = patient.DateOfBirth;
                    vm.PhoneNumber = patient.PhoneNumber;
                    vm.Address = patient.Address;
                    if (patient.MedicalProfile != null)
                    {
                        vm.BloodGroup = patient.MedicalProfile.BloodGroup;
                        vm.KnownAllergies = patient.MedicalProfile.KnownAllergies;
                        vm.ChronicConditions = patient.MedicalProfile.ChronicConditions;
                    }
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                vm.Role = "Doctor";
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == user.Id);
                if (doctor != null)
                {
                    vm.SpecializationId = doctor.SpecializationId;
                    vm.LicenseNumber = doctor.LicenseNumber;
                    vm.YearsOfExperience = doctor.YearsOfExperience;
                    vm.ConsultationFee = doctor.ConsultationFee;
                    vm.AvailableFrom = doctor.AvailableFrom;
                    vm.AvailableTo = doctor.AvailableTo;
                    vm.SlotDurationMinutes = doctor.SlotDurationMinutes;
                    vm.ProfilePictureUrl = doctor.ProfilePictureUrl;
                    vm.Education = doctor.Education;
                    vm.IsOnLeave = doctor.IsOnLeave;
                }
                ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");
            }
            else
            {
                vm.Role = "Admin";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (User.IsInRole("Doctor"))
            {
                ViewBag.Specializations = new SelectList(await _context.Specializations.ToListAsync(), "Id", "Name");
            }

            if (!ModelState.IsValid) return View(vm);

            user.FullName = vm.FullName;
            await _userManager.UpdateAsync(user);

            if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients.Include(p => p.MedicalProfile).FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
                if (patient != null)
                {
                    patient.DateOfBirth = vm.DateOfBirth;
                    patient.PhoneNumber = vm.PhoneNumber;
                    patient.Address = vm.Address;

                    if (patient.MedicalProfile == null)
                    {
                        patient.MedicalProfile = new MedicalProfile { PatientId = patient.Id };
                    }
                    patient.MedicalProfile.BloodGroup = vm.BloodGroup;
                    patient.MedicalProfile.KnownAllergies = vm.KnownAllergies;
                    patient.MedicalProfile.ChronicConditions = vm.ChronicConditions;
                    patient.MedicalProfile.LastUpdated = DateTime.UtcNow;

                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == user.Id);
                if (doctor != null)
                {
                    if (vm.SpecializationId.HasValue) doctor.SpecializationId = vm.SpecializationId.Value;
                    if (!string.IsNullOrEmpty(vm.LicenseNumber)) doctor.LicenseNumber = vm.LicenseNumber;
                    if (vm.YearsOfExperience.HasValue) doctor.YearsOfExperience = vm.YearsOfExperience.Value;
                    if (vm.ConsultationFee.HasValue) doctor.ConsultationFee = vm.ConsultationFee.Value;
                    if (vm.AvailableFrom.HasValue) doctor.AvailableFrom = vm.AvailableFrom.Value;
                    if (vm.AvailableTo.HasValue) doctor.AvailableTo = vm.AvailableTo.Value;
                    if (vm.SlotDurationMinutes.HasValue) doctor.SlotDurationMinutes = vm.SlotDurationMinutes.Value;
                    
                    doctor.Education = vm.Education;

                    if (vm.ProfileImage != null)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "profiles");
                        Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + vm.ProfileImage.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await vm.ProfileImage.CopyToAsync(fileStream);
                        }
                        doctor.ProfilePictureUrl = "/images/profiles/" + uniqueFileName;
                    }

                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = $"User {user.Email} updated their profile"
            });
            await _context.SaveChangesAsync();

            TempData["Message"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
