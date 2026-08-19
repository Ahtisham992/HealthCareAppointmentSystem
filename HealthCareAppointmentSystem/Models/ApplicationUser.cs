using Microsoft.AspNetCore.Identity;

namespace HealthCareAppointmentSystem.Models
{
    /// <summary>
    /// Extends the default Identity user with a FullName field.
    /// This is the login account shared by Admins, Doctors, and Patients -
    /// role-specific profile data lives in the Doctor/Patient tables.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
