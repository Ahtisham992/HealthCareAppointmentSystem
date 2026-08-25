using Stripe.Checkout;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HealthCareAppointmentSystem.Services
{
    public class StripePaymentService : IStripePaymentService
    {
        public StripePaymentService(IConfiguration configuration)
        {
            Stripe.StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
        }

        public async Task<string> CreateCheckoutSessionAsync(decimal amount, string referenceId, string returnUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(amount * 100), // Stripe expects amount in lowest denominator
                            Currency = "pkr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Wallet Deposit",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = returnUrl + "?sessionId={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                ClientReferenceId = referenceId
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return session.Url;
        }

        public async Task<bool> IsPaymentSuccessfulAsync(string sessionId)
        {
            try 
            {
                var service = new SessionService();
                var session = await service.GetAsync(sessionId);
                return session.PaymentStatus == "paid";
            }
            catch
            {
                return false;
            }
        }
    }
}
