using System;
using System.ComponentModel.DataAnnotations;

namespace Tracklio.Shared.Domain.Dto.Payments;

public class CreateSubscriptionRequest
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    [StringLength(10)]
    public string BillingPeriod { get; set; } = "monthly"; // monthly, yearly

    [Required]
    public string PaymentMethodId { get; set; } = string.Empty;
}

public class CreatePaymentIntentRequest
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    [StringLength(10)]
    public string BillingPeriod { get; set; } = "monthly";
}

public class PaymentIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
}

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string ExternalSubscriptionId { get; set; } = string.Empty;
}

public class StripeWebhookEvent
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object Data { get; set; } = null!;
}

