using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class Patient
    {
        public int Id { get; set; }

        // Links this Patient profile to its login account (1:1)
        [Required]
        [RegularExpression(@"^[0-9]{5}-[0-9]{7}-[0-9]{1}$", ErrorMessage = "CNIC format must be XXXXX-XXXXXXX-X")]
        [Display(Name = "CNIC Number")]
        [StringLength(15)]
        public string CNIC { get; set; } = string.Empty;

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Phone, StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        public int CancellationCount { get; set; } = 0;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        public MedicalProfile? MedicalProfile { get; set; }
    }
}
