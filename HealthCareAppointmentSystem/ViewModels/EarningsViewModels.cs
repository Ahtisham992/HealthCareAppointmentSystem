using System;
using System.Collections.Generic;
using HealthCareAppointmentSystem.Models;

namespace HealthCareAppointmentSystem.ViewModels
{
    public class AdminEarningsIndexViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<DoctorEarningRow> Doctors { get; set; } = new List<DoctorEarningRow>();
        public decimal TotalPlatformEarnings { get; set; }
    }

    public class DoctorEarningRow
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public decimal TotalEarned { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal NetEarned { get; set; }
        public decimal Commission { get; set; }
        
        public decimal BilledEarnings { get; set; }
        public decimal BilledCommission { get; set; }
        public decimal UnbilledEarnings { get; set; }
        public decimal UnbilledCommission { get; set; }
        
        public bool IsFullyBilled => UnbilledEarnings <= 0;
        public bool HasPendingBills { get; set; }
        public bool HasSubmittedBills { get; set; }
        
        // Keep track of the most recent bill for action rendering
        public BillStatus? LatestBillStatus { get; set; }
        public int? LatestBillId { get; set; }
    }

    public class DoctorEarningsViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalEarnedThisMonth { get; set; }
        public decimal TotalRefundedThisMonth { get; set; }
        public decimal TotalPlatformFeesPaid { get; set; }
        public decimal NetEarnedThisMonth { get; set; }
        public List<EarningLogViewModel> Logs { get; set; } = new List<EarningLogViewModel>();
    }

    public class EarningLogViewModel
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Payment"; // "Payment" or "Refund"
        public string BadgeClass => Type == "Payment" ? "status-completed" : "status-cancelled";
    }
}
