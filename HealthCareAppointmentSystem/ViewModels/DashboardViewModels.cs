using HealthCareAppointmentSystem.Models;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalAppointments { get; set; }
    }

    public class DoctorDashboardViewModel
    {
        public Doctor? DoctorProfile { get; set; }
        public int TodayAppointmentsCount { get; set; }
        public int UpcomingAppointmentsCount { get; set; }
        public int PendingConfirmationsCount { get; set; }
        public int PendingCompletionsCount { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; } = new();
    }

    public class PatientDashboardViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int? SpecializationId { get; set; }
        public int PendingReviewsCount { get; set; }
        public List<Doctor> AvailableDoctors { get; set; } = new();
    }
}
