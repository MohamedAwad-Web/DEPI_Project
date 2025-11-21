using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Bookify.Services.Services
{
    public class PaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly string _webBaseUrl;

        public PaymentService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;

            var request = httpContextAccessor.HttpContext?.Request;
            _webBaseUrl = request != null ? $"{request.Scheme}://{request.Host}" : string.Empty;

            // Initialize Stripe with real API key
            var secretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("Stripe Secret Key is not configured.");
            }

            StripeConfiguration.ApiKey = secretKey;
        }

        public Session CreateCheckoutSession(decimal totalAmount, string bookingDescription,
                                           string successUrl, string cancelUrl, Dictionary<string, string>? metadata = null)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(successUrl))
                    throw new ArgumentException("Success URL is required", nameof(successUrl));

                if (string.IsNullOrEmpty(cancelUrl))
                    throw new ArgumentException("Cancel URL is required", nameof(cancelUrl));

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Hotel Room Booking",
                                    Description = bookingDescription ?? "Hotel room reservation",
                                },
                                UnitAmount = (long)(totalAmount * 100), // Convert to cents
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Metadata = metadata ?? new Dictionary<string, string>(),
                    CustomerEmail = metadata?.GetValueOrDefault("customer_email"),
                };

                var service = new SessionService();
                var session = service.Create(options);

                return session;
            }
            catch (StripeException ex)
            {
                throw new Exception($"Stripe error: {ex.Message}", ex);
            }
        }

        public Session GetCheckoutSession(string sessionId)
        {
            try
            {
                var service = new SessionService();
                return service.Get(sessionId);
            }
            catch (StripeException ex)
            {
                throw new Exception($"Stripe error: {ex.Message}", ex);
            }
        }

        public bool IsPaymentSuccessful(string sessionId)
        {
            try
            {
                var session = GetCheckoutSession(sessionId);
                return session.PaymentStatus == "paid";
            }
            catch
            {
                return false;
            }
        }
    }
}