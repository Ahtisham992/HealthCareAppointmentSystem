using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public enum TransactionType
    {
        Deposit = 1,          // User added funds via Stripe
        Withdrawal = 2,       // User withdrew funds to bank account
        ServicePayment = 3,   // User paid for appointment/medicine (Debit)
        ServiceEarning = 4,   // Doctor/Pharmacist received payment (Credit)
        PlatformCommission = 5, // Platform cut from services (Credit to platform)
        WithdrawalFee = 6,    // Fee charged for withdrawal (Debit to user, Credit to platform)
        CashDeposit = 7       // Physical cash deposited by Receptionist
    }

    public class WalletTransaction
    {
        public int Id { get; set; }

        [Required]
        public int WalletId { get; set; }

        [ForeignKey(nameof(WalletId))]
        public Wallet? Wallet { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // Can be negative for debit

        public TransactionType Type { get; set; }

        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ReferenceId { get; set; } // E.g. "INV-001", "RX-005"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
