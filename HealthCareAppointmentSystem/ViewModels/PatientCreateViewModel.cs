using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class PatientCreateViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
    }
}
