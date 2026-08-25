using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCareAppointmentSystem.Models
{
    public enum AppointmentStatus
    {
        Pending = 0,
        Confirmed = 1,
        Completed = 2,
        Cancelled = 3,
        PatientCancellationRequested = 4,
        DoctorCancellationRequested = 5,
        PatientRefundVerificationPending = 6
    }

    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey(nameof(DoctorId))]
        public Doctor? Doctor { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        [Required]
        [Display(Name = "Appointment Date & Time")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDateTime { get; set; }

        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public bool IsRefunded { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        public Invoice? Invoice { get; set; }

        public Prescription? Prescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
