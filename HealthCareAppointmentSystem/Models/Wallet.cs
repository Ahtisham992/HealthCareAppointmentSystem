using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class Wallet
    {
        public int Id { get; set; }

        public string? ApplicationUserId { get; set; } // Null implies this is the central Platform/Escrow wallet

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0m;

        [StringLength(10)]
        public string Currency { get; set; } = "PKR";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
