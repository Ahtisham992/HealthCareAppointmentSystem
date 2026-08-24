using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class ReceptionistDashboardViewModel
    {
        public decimal CashDrawerBalance { get; set; }
        public int PendingInvoicesCount { get; set; }
        public int TodayAppointmentsCount { get; set; }
        public List<HealthCareAppointmentSystem.Models.Appointment> UpcomingAppointments { get; set; } = new();
    }

    public class BookAppointmentViewModel
    {
        [Required]
        [RegularExpression(@"^[0-9]{5}-[0-9]{7}-[0-9]{1}$", ErrorMessage = "CNIC format must be XXXXX-XXXXXXX-X")]
        [Display(Name = "Patient CNIC")]
        public string CNIC { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required]
        [Display(Name = "Appointment Date & Time")]
        public DateTime AppointmentDateTime { get; set; }
        
        // These fields are only used if the CNIC is NOT found and we need to register them on the spot
        public string? NewPatientFullName { get; set; }
        public string? NewPatientEmail { get; set; }
        public string? NewPatientPhone { get; set; }
        public DateTime? NewPatientDOB { get; set; }
    }
}
