using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public enum PrescriptionStatus
    {
        Pending = 0,
        Processing = 1,
        Dispensed = 2
    }

    public class Prescription
    {
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment? Appointment { get; set; }

        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Pending;

        [StringLength(500)]
        public string? DoctorNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DispensedAt { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; } = 0;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public DateTime? PaidAt { get; set; }

        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }
}
