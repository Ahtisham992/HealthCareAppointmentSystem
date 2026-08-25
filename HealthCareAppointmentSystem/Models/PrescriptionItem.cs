using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class PrescriptionItem
    {
        public int Id { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        [ForeignKey(nameof(PrescriptionId))]
        public Prescription? Prescription { get; set; }

        [Required]
        [StringLength(100)]
        public string MedicineName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Dosage { get; set; } = null!; // e.g., "500mg"

        [Required]
        [StringLength(50)]
        public string Frequency { get; set; } = null!; // e.g., "1x a day"

        [Required]
        [StringLength(50)]
        public string Duration { get; set; } = null!; // e.g., "5 days"

        [StringLength(200)]
        public string? SpecialInstructions { get; set; } // e.g., "After meals"

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; } = 0;
    }
}
