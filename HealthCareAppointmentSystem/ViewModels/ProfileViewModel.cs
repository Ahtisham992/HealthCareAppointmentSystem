using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class ProfileViewModel
    {
        public string Role { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Patient fields
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        // Doctor fields
        [Display(Name = "Specialization")]
        public int? SpecializationId { get; set; }

        public string? SpecializationName { get; set; }

        [Display(Name = "License Number")]
        public string? LicenseNumber { get; set; }

        [Display(Name = "Years of Experience")]
        public int? YearsOfExperience { get; set; }

        [Display(Name = "Consultation Fee")]
        public decimal? ConsultationFee { get; set; }

        public bool IsApproved { get; set; }

        [Display(Name = "Available From")]
        public TimeSpan? AvailableFrom { get; set; }

        [Display(Name = "Available To")]
        public TimeSpan? AvailableTo { get; set; }

        [Display(Name = "Slot Duration (Mins)")]
        public int? SlotDurationMinutes { get; set; }
    }
}
