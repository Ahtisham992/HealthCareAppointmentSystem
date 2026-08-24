using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class CashHandover
    {
        public int Id { get; set; }

        [Required]
        public int ReceptionistId { get; set; }

        [ForeignKey(nameof(ReceptionistId))]
        public Receptionist? Receptionist { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DateTime HandoverDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string AdminUserId { get; set; } = string.Empty; // Admin who accepted it

        [ForeignKey(nameof(AdminUserId))]
        public ApplicationUser? AdminUser { get; set; }
    }
}
