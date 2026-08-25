using System.Threading.Tasks;

namespace HealthCareAppointmentSystem.Services
{
    public interface IStripePaymentService
    {
        Task<string> CreateCheckoutSessionAsync(decimal amount, string referenceId, string returnUrl, string cancelUrl);
        Task<bool> IsPaymentSuccessfulAsync(string sessionId);
    }
}
