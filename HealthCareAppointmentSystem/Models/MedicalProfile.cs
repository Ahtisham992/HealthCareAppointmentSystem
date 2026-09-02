using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class MedicalProfile
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        [StringLength(10)]
        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; } // e.g. "O+", "A-"

        [StringLength(500)]
        [Display(Name = "Known Allergies")]
        public string? KnownAllergies { get; set; } // Comma separated or descriptive text

        [StringLength(500)]
        [Display(Name = "Chronic Conditions")]
        public string? ChronicConditions { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
