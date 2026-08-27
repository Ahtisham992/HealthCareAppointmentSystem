using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public enum WithdrawalStatus
    {
        Pending = 0,
        Completed = 1,
        Rejected = 2
    }

    public class WithdrawalRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WalletId { get; set; }
        
        [ForeignKey("WalletId")]
        public Wallet? Wallet { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string BankDetails { get; set; } = string.Empty;

        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessedByAccountId { get; set; }

        public string? Notes { get; set; }
    }
}
