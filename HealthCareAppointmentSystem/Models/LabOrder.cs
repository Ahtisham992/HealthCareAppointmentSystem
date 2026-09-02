using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class LabOrder
    {
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment? Appointment { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Test Type")]
        public string TestType { get; set; } = string.Empty; // e.g. "Complete Blood Count"

        [StringLength(500)]
        public string? Notes { get; set; } // Doctor's instructions

        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Cost")]
        public decimal Cost { get; set; } = 0;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [StringLength(50)]
        public string? SampleId { get; set; }

        public bool IsCompleted { get; set; } = false;

        public LabResult? LabResult { get; set; }
    }
}
