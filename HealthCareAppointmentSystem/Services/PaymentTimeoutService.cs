using HealthCareAppointmentSystem.Data;
using HealthCareAppointmentSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthCareAppointmentSystem.Services
{
    public class PaymentTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentTimeoutService> _logger;

        public PaymentTimeoutService(IServiceProvider serviceProvider, ILogger<PaymentTimeoutService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment Timeout Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelUnpaidAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing PaymentTimeoutService.");
                }

                // Wait 1 minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Payment Timeout Background Service is stopping.");
        }

        private async Task CancelUnpaidAppointmentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);

            var expiredAppointments = await context.Appointments
                .Include(a => a.Invoice)
                .Where(a => a.Status == AppointmentStatus.Pending 
                            && a.CreatedAt <= tenMinutesAgo 
                            && a.Invoice != null 
                            && a.Invoice.Status == PaymentStatus.Pending)
                .ToListAsync();

            if (expiredAppointments.Any())
            {
                foreach (var appointment in expiredAppointments)
                {
                    appointment.Status = AppointmentStatus.Cancelled;
                    appointment.CancellationReason = "Auto-cancelled: Payment not submitted within 10 minutes.";
                    
                    if (appointment.Invoice != null)
                    {
                        appointment.Invoice.Status = PaymentStatus.Failed;
                    }

                    context.AuditLogs.Add(new AuditLog
                    {
                        Action = "System Auto-Cancel",
                        UserId = "System",
                        Details = $"Appointment #{appointment.Id} auto-cancelled due to payment timeout."
                    });
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"Auto-cancelled {expiredAppointments.Count} unpaid appointments.");
            }
        }
    }
}
