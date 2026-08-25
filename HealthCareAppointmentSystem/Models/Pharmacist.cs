using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class Pharmacist
    {
        public int Id { get; set; }
        
        [Required]
        public string ApplicationUserId { get; set; } = null!;
        
        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
