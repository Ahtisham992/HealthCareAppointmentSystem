using System.ComponentModel.DataAnnotations;
using HealthCareAppointmentSystem.Models;

namespace HealthCareAppointmentSystem.ViewModels
{
    /// <summary>
    /// Shapes the data for the Create/Edit Appointment forms -
    /// keeps the Views decoupled from the raw EF entity.
    /// </summary>
    public class AppointmentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a doctor.")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        // Only used/visible when an Admin is booking on behalf of a patient;
        // for a logged-in Patient, this is set automatically from their account.
        [Display(Name = "Patient")]
        public int? PatientId { get; set; }

        [Required(ErrorMessage = "Please choose a date and time.")]
        [Display(Name = "Appointment Date & Time")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDateTime { get; set; } = DateTime.Now.AddDays(1);

        [StringLength(500)]
        public string? Notes { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        // Dropdown data
        public List<Doctor> AvailableDoctors { get; set; } = new();
        public List<Patient> AvailablePatients { get; set; } = new();
    }
}
