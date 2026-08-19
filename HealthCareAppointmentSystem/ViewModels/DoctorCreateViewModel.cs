using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class DoctorCreateViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Specialization")]
        public int SpecializationId { get; set; }

        [Required]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Years Of Experience")]
        [Range(0, 60)]
        public int YearsOfExperience { get; set; }

        [Required]
        [Display(Name = "Consultation Fee")]
        [Range(0, 100000)]
        public decimal ConsultationFee { get; set; }
    }
}
