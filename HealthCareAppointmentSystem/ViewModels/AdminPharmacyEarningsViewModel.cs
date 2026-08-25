using System.Collections.Generic;
using HealthCareAppointmentSystem.Models;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class AdminPharmacyEarningsViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }

        public decimal TotalPharmacySales { get; set; }
        public decimal TotalPlatformFees { get; set; }

        public List<Prescription> PaidPrescriptions { get; set; } = new List<Prescription>();
    }
}
