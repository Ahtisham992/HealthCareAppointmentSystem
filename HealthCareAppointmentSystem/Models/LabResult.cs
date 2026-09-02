using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public class LabResult
    {
        public int Id { get; set; }

        [Required]
        public int LabOrderId { get; set; }

        [ForeignKey(nameof(LabOrderId))]
        public LabOrder? LabOrder { get; set; }

        [Required]
        public int LabTechnicianId { get; set; }

        [ForeignKey(nameof(LabTechnicianId))]
        public LabTechnician? LabTechnician { get; set; }

        [StringLength(1000)]
        public string? ResultNotes { get; set; }

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty; // URL or Path to uploaded PDF

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
