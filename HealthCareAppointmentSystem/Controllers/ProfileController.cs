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

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
                if (patient != null)
                {
                    vm.DateOfBirth = patient.DateOfBirth;
                    vm.PhoneNumber = patient.PhoneNumber;
                    vm.Address = patient.Address;
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
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
                if (patient != null)
                {
                    vm.DateOfBirth = patient.DateOfBirth;
                    vm.PhoneNumber = patient.PhoneNumber;
                    vm.Address = patient.Address;
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
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
                if (patient != null)
                {
                    patient.DateOfBirth = vm.DateOfBirth;
                    patient.PhoneNumber = vm.PhoneNumber;
                    patient.Address = vm.Address;
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
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Message"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
