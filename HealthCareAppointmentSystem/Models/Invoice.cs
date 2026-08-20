using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public enum PaymentStatus
    {
        Pending = 0,
        AwaitingVerification = 1,
        Paid = 2,
        Failed = 3,
        Refunded = 4
    }

    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment? Appointment { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(50)]
        public string? PaymentMethod { get; set; } // e.g., "Bank Transfer", "1Link"

        [StringLength(100)]
        public string? TransactionReference { get; set; } // Entered by patient

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }
}
