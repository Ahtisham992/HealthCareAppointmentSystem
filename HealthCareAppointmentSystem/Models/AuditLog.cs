using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Store user ID or Name

        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;

        public string? Details { get; set; } // JSON or text

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
