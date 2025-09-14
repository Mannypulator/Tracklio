using System;
using Tracklio.Shared.Domain.Dto.Payments;
using Tracklio.Shared.Domain.Entities;

namespace Tracklio.Shared.Services.Stripe;

public interface IStripeService
{
    Task<PaymentIntentResponse> CreatePaymentIntentAsync(Guid userId, CreatePaymentIntentRequest request);
    Task<SubscriptionResponse> CreateSubscriptionAsync(Guid userId, CreateSubscriptionRequest request);
    Task<SubscriptionResponse?> GetSubscriptionAsync(string subscriptionId);
    Task<SubscriptionResponse?> CancelSubscriptionAsync(string subscriptionId);
    Task<bool> HandleWebhookAsync(string payload, string signature);
    Task<string> CreateCustomerAsync(User user);
}
