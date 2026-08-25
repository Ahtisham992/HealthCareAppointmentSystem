using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        // Links this Doctor profile to its login account (1:1)
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{5}-[0-9]{7}-[0-9]{1}$", ErrorMessage = "CNIC format must be XXXXX-XXXXXXX-X")]
        [Display(Name = "CNIC Number")]
        [StringLength(15)]
        public string CNIC { get; set; } = string.Empty;

        [Required]
        public int SpecializationId { get; set; }

        [ForeignKey(nameof(SpecializationId))]
        public Specialization? Specialization { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Range(0, 60)]
        [Display(Name = "Years of Experience")]
        public int YearsOfExperience { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Consultation Fee")]
        public decimal ConsultationFee { get; set; }

        public bool IsApproved { get; set; } = false;

        [Display(Name = "Available From")]
        public TimeSpan AvailableFrom { get; set; } = new TimeSpan(9, 0, 0); // Default 9 AM

        [Display(Name = "Available To")]
        public TimeSpan AvailableTo { get; set; } = new TimeSpan(17, 0, 0); // Default 5 PM

        [Display(Name = "Slot Duration (Mins)")]
        public int SlotDurationMinutes { get; set; } = 20;

        public string? ProfilePictureUrl { get; set; }

        public string? Education { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<PlatformBill> PlatformBills { get; set; } = new List<PlatformBill>();
    }
}
