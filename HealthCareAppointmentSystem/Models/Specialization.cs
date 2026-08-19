using System.ComponentModel.DataAnnotations;

namespace HealthCareAppointmentSystem.Models
{
    public class Specialization
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}
