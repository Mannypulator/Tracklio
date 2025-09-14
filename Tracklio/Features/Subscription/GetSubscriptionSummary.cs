using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Subscription;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Admin;

public class GetSubscriptionSummary : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // complete the route check previous examples
        endpointRouteBuilder.MapGet("/api/v1/subscription/summary", async ([FromServices] IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSubscriptionSummaryQuery());
                return result.ReturnedResponse();
            })
            .WithName("GetSubscriptionSummary")
            .WithTags("Subscriptions")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get Subscription Summary",
                Description = "Retrieve the subscription summary.",
                OperationId = "GetSubscriptionSummary",
            })
            .Produces<GenericResponse<SubscriptionSummaryDto>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<SubscriptionSummaryDto>>(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy");
    }

    public record GetSubscriptionSummaryQuery() : IRequest<GenericResponse<SubscriptionSummaryDto>>;

    public class GetSubscriptionSummaryQueryHandler(RepositoryContext context) : IRequestHandler<GetSubscriptionSummaryQuery, GenericResponse<SubscriptionSummaryDto>>
    {
        public async Task<GenericResponse<SubscriptionSummaryDto>> Handle(GetSubscriptionSummaryQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            var totalUsers = await context.Users.AsNoTracking().CountAsync(cancellationToken: cancellationToken);
            var activeSubscribers = await context.UserSubscriptions
                .Where(s => s.Status == "Active" && s.EndDate == null)
                .CountAsync(cancellationToken: cancellationToken);
            var freemiumUsers = await context.Users
                .CountAsync(u => u.Role == UserRole.Motorist && !u.HasSubscription, cancellationToken: cancellationToken);

            var result = new SubscriptionSummaryDto
            {
                TotalUsers = totalUsers,
                ActiveSubscribers = activeSubscribers,
                FreemiumUsers = freemiumUsers,
                UserGrowthRate = 9.0,
                SubscriberGrowthRate = -12.0,
                FreemiumGrowthRate = 9.0
            };

            return GenericResponse<SubscriptionSummaryDto>.Success("Success", result);
        }
    }
}
