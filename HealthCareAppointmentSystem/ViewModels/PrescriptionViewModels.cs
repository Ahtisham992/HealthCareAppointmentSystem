using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class CreatePrescriptionViewModel
    {
        [Required]
        public int AppointmentId { get; set; }

        public string? PatientName { get; set; }
        public DateTime? AppointmentDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Doctor's Notes")]
        public string? DoctorNotes { get; set; }

        public List<PrescriptionItemViewModel> Items { get; set; } = new List<PrescriptionItemViewModel>();
    }

    public class PrescriptionItemViewModel
    {
        [Required(ErrorMessage = "Medicine name is required")]
        [StringLength(100)]
        public string MedicineName { get; set; } = null!;

        [Required(ErrorMessage = "Dosage is required")]
        [StringLength(50)]
        public string Dosage { get; set; } = null!;

        [Required(ErrorMessage = "Frequency is required")]
        [StringLength(50)]
        public string Frequency { get; set; } = null!;

        [Required(ErrorMessage = "Duration is required")]
        [StringLength(50)]
        public string Duration { get; set; } = null!;

        [StringLength(200)]
        public string? SpecialInstructions { get; set; }
    }
}
