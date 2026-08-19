namespace HealthCareAppointmentSystem.ViewModels
{
    public class AccountViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
    }
}
