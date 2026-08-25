using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class AccountViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
    }

    public class AccountCreateViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [RegularExpression(@"^[0-9]{5}-[0-9]{7}-[0-9]{1}$", ErrorMessage = "CNIC format must be XXXXX-XXXXXXX-X")]
        [Display(Name = "CNIC Number (Required for Doctors and Patients)")]
        [StringLength(15)]
        public string? CNIC { get; set; }

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

    public class AccountEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "New Password (Optional)")]
        public string? NewPassword { get; set; }
    }
}
