using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class Receptionist
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Cash Drawer Balance")]
        public decimal CashDrawerBalance { get; set; } = 0;

        public ICollection<CashHandover> CashHandovers { get; set; } = new List<CashHandover>();
        public ICollection<Invoice> CollectedInvoices { get; set; } = new List<Invoice>();
    }
}
