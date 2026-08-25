using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public enum BillStatus
    {
        Pending = 0,
        PaymentSubmitted = 1,
        Paid = 2, // Actually VerifiedAndPaid but we can keep it as Paid for backward compatibility and display it as Verified
        Cancelled = 3
    }

    public class PlatformBill
    {
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public Doctor? Doctor { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalEarnings { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal CommissionAmount { get; set; }

        public BillStatus Status { get; set; } = BillStatus.Pending;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
        
        [StringLength(100)]
        public string? TransactionReference { get; set; }
        
        public DateTime? SubmittedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}
