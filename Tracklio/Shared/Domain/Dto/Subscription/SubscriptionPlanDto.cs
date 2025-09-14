using System;

namespace Tracklio.Shared.Domain.Dto.Subscription;

public class SubscriptionPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? PriceYearly { get; set; }
    public string Currency { get; set; } = "GBP";
    public string BillingPeriod { get; set; } = "monthly";
    public string Description { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public int MaxVehicles { get; set; }
    public bool IsPopular { get; set; }
    public bool ContactSupport { get; set; }
}


public class SubscriptionSummaryDto
{
    public int TotalUsers { get; set; }
    public int ActiveSubscribers { get; set; }
    public int FreemiumUsers { get; set; }
    public double UserGrowthRate { get; set; }
    public double SubscriberGrowthRate { get; set; }
    public double FreemiumGrowthRate { get; set; }
}

public class UserSubscriptionDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentPlan { get; set; } = string.Empty;
    public int VehiclesUsed { get; set; }
    public int VehiclesAllowed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? RenewalDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string ProfileImage { get; set; } = string.Empty;
}