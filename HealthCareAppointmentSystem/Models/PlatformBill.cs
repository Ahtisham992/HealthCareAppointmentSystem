using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public enum BillStatus
    {
        Pending = 0,
        Paid = 1,
        Cancelled = 2
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

        public DateTime? PaidAt { get; set; }
    }
}
