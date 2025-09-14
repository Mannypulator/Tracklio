using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Subscription;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Subscription;

public class GetPlans : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/api/v1/subscription/plans", async ([FromQuery] string billingPeriod, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var response = await mediator.Send(new GetPlansQuery(billingPeriod), cancellationToken);
                    return response.ReturnedResponse();
                })
                .WithTags("Subscriptions")
                .WithName("GetSubscriptionPlans")
                .WithOpenApi(op =>
                {
                    op.Summary = "Get Subscription Plans";
                    op.Description = "Retrieve available subscription plans based on the specified billing period (monthly or yearly).";
                    op.Parameters[0].Description = "Billing period for the subscription plans (monthly or yearly).";
                    op.Responses["200"].Description = "A list of subscription plans.";
                    return op;
                });
    }

    public record GetPlansQuery(string BillingPeriod) : IRequest<GenericResponse<IReadOnlyList<SubscriptionPlanDto>>>;

    public class GetPlansQueryHandler(RepositoryContext context) : IRequestHandler<GetPlansQuery, GenericResponse<IReadOnlyList<SubscriptionPlanDto>>>
    {
        public async Task<GenericResponse<IReadOnlyList<SubscriptionPlanDto>>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
        {
            var plans = await context.SubscriptionPlans
                .AsNoTracking()
                .Select(p => new SubscriptionPlanDto
                {
                    Id = p.Name,
                    Name = p.DisplayName,
                    Icon = p.Icon,
                    Price = request.BillingPeriod == "yearly" ? p.PriceYearly / 12 : p.PriceMonthly,
                    PriceYearly = p.PriceYearly,
                    Currency = p.Currency,
                    BillingPeriod = request.BillingPeriod,
                    Description = p.Description,
                    Features = GetFeaturesForPlan(p.Name),
                    MaxVehicles = p.MaxVehicles,
                    IsPopular = p.IsPopular,
                    ContactSupport = p.Name == "enterprise"
                })
                .ToListAsync(cancellationToken);

            return GenericResponse<IReadOnlyList<SubscriptionPlanDto>>.Success("Subscription plans retrieved successfully", plans);
        }

        private static List<string> GetFeaturesForPlan(string planId)
        {
            return planId switch
            {
                "freemium" => new List<string> { "Covers one vehicle", "In app push notification PCNs alert", "Car park locator and map" },
                "solo" or "family" or "fleet" => new List<string>
            {
                "Ads free",
                $"Covers up to {(planId == "solo" ? 5 : planId == "family" ? 10 : 15)} vehicles",
                "Email, sms & in app push notification PCNs alert",
                "Active MOT checks"
            },
                "enterprise" =>
            [
                "Fleet of vehicles 0 to 15+ vehicles",
                "Email, sms & in app push notification PCNs alert",
                "Active MOT checks"
            ],
                _ => []
            };
        }
    }
}
