using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Subscription;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Subscription;

public class GetCurrentSubscription : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/api/v1/subscription/current", async ([FromQuery] Guid userId, [FromServices] IMediator mediator) =>
            {
                var result = await mediator.Send(new GetCurrentSubscriptionQuery(userId));
                return result.ReturnedResponse();
            })
            .WithTags("Subscriptions")
            .WithName("GetCurrentSubscription")
            .Produces<GenericResponse<CurrentSubscriptionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    public record GetCurrentSubscriptionQuery(Guid UserId) : IRequest<GenericResponse<CurrentSubscriptionDto>>;

    public class GetCurrentSubscriptionQueryHandler(RepositoryContext context) : IRequestHandler<GetCurrentSubscriptionQuery, GenericResponse<CurrentSubscriptionDto>>
    {
        public async Task<GenericResponse<CurrentSubscriptionDto>> Handle(GetCurrentSubscriptionQuery request, CancellationToken cancellationToken)
        {
            var user = await context.Users
                .AsNoTracking()
                .Include(u => u.Subscriptions)
                    .ThenInclude(us => us.Plan)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return GenericResponse<CurrentSubscriptionDto>.Error(401, "User not found");
            }

            var activeSub = user.Subscriptions
                .Where(us => us.UserId == request.UserId && us.EndDate == null || us.EndDate > DateTime.UtcNow)
                .OrderByDescending(us => us.StartDate)
                .FirstOrDefault();

            SubscriptionPlanDto currentPlanDto;
            bool isSubscribed = activeSub != null;



            if (isSubscribed)
            {
                var plan = activeSub.Plan;
                currentPlanDto = new SubscriptionPlanDto
                {
                    Id = plan.Name,
                    Name = plan.DisplayName,
                    Icon = plan.Icon,
                    Price = activeSub.BillingPeriod == "yearly" ? plan.PriceYearly / 12 : plan.PriceMonthly,
                    PriceYearly = plan.PriceYearly,
                    Currency = plan.Currency,
                    BillingPeriod = activeSub.BillingPeriod,
                    Description = plan.Description,
                    Features = GetFeaturesForPlan(plan.Name),
                    MaxVehicles = plan.MaxVehicles,
                    IsPopular = plan.IsPopular
                };
            }
            else
            {
                // Default to freemium
                var freemium = await context.SubscriptionPlans.FirstAsync(p => p.Name == "freemium");
                currentPlanDto = new SubscriptionPlanDto
                {
                    Id = "freemium",
                    Name = "Freemium",
                    Icon = "🆓",
                    Price = 0,
                    PriceYearly = 0,
                    Currency = "GBP",
                    BillingPeriod = "monthly",
                    Description = "Covers one vehicle",
                    Features = GetFeaturesForPlan("freemium"),
                    MaxVehicles = 1
                };
            }

            return GenericResponse<CurrentSubscriptionDto>.Success("Current subscription retrieved successfully", new CurrentSubscriptionDto
            {
                CurrentPlan = currentPlanDto,
                IsSubscribed = user.HasSubscription,
                NextBillingDate = null,
                CancellationAllowed = true
            });
        }
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
