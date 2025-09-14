using System;

namespace Tracklio.Shared.Domain.Dto.Subscription;

public class CurrentSubscriptionDto
{
    public SubscriptionPlanDto CurrentPlan { get; set; } = null!;
    public bool IsSubscribed { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public bool CancellationAllowed { get; set; }
}
