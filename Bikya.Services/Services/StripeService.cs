using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.DTOs.StripeDTOs;
using Bikya.Services.Interfaces;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

public class StripeService : IStripeService
{
    private readonly StripeSettings _settings;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;

    public StripeService(IOptions<StripeSettings> settings, IOrderRepository orderRepo, IPaymentRepository paymentRepo)
    {
        _settings = settings.Value;
        _orderRepository = orderRepo;
        _paymentRepository = paymentRepo;

        Stripe.StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<StripeSessionInfo> CreateCheckoutSessionAsync(decimal amount, int orderId)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
        {
            new()
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(amount * 100),
                    Currency = "egp",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Order #{orderId}"
                    }
                },
                Quantity = 1
            }
        },
            Mode = "payment",
            SuccessUrl = $"http://localhost:4200/payment/success?session_id={{CHECKOUT_SESSION_ID}}",  // ✅ Angular localhost port
            CancelUrl = $"http://localhost:4200/payment/cancel",
            Metadata = new Dictionary<string, string>
        {
            { "order_id", orderId.ToString() }
        }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return new StripeSessionInfo
        {
            Id = session.Id,
            Url = session.Url
        };
    }

}
