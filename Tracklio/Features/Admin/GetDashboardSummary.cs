using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Admin;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Admin;

public class GetDashboardSummary : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/admin/dashboard-summary", async (
                [FromServices] IMediator mediator,
                [FromBody] GetDashboardSummaryQuery query,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(query, ct);
                return response.ReturnedResponse();
            })
            .WithName("GetDashboardSummary")
            .WithTags("Admin")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get Dashboard Summary",
                Description = "Get summary statistics for the admin dashboard.",
                OperationId = "GetDashboardSummary",
            })
            .Produces<GenericResponse<DashboardSummaryDto>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }

    public record GetDashboardSummaryQuery(string? Month, int? Year) : IRequest<GenericResponse<DashboardSummaryDto>>;


    public class GetDashboardSummaryQueryHandler(RepositoryContext context) : IRequestHandler<GetDashboardSummaryQuery, GenericResponse<DashboardSummaryDto>>
    {
        public async Task<GenericResponse<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            // For simplicity, ignoring Month and Year filtering in this example
            
            var now = DateTime.UtcNow;
            var targetMonth = request.Month != null ? DateTime.ParseExact(request.Month, "MMMM", null).Month : now.Month;
            var startOfMonth = request.Year != null ? new DateTime(request.Year.Value, targetMonth, 1) : new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var targetYear = request.Year ?? now.Year;

            var startOfLastYear = new DateTime(targetYear - 1, 1, 1);
            var startOfCurrentYear = new DateTime(targetYear, 1, 1);

            var users = await context.Users.AsNoTracking().Where(u => u.IsActive).ToListAsync(cancellationToken: cancellationToken);
            var vehicles = await context.Vehicles.AsNoTracking().Where(v => v.IsActive).ToListAsync(cancellationToken: cancellationToken);
            var tickets = await context.ParkingTickets.AsNoTracking().ToListAsync(cancellationToken: cancellationToken);

            // Growth logic (simplified: count created in current vs last month)
            Func<DateTime, IEnumerable<dynamic>, int> countInMonth = (date, items) => items.Count(i => i.CreatedAt >= date);
            Func<DateTime, IEnumerable<dynamic>, int> countLastMonth = (date, items) => items.Count(i => i.CreatedAt >= date && i.CreatedAt < startOfMonth);

            var currentUsers = countInMonth(startOfMonth, users);
            var lastMonthUsers = countLastMonth(startOfLastMonth, users);
            var userGrowthRate = lastMonthUsers == 0 ? (currentUsers > 0 ? 100 : 0) : (double)(currentUsers - lastMonthUsers) / lastMonthUsers * 100;

            var subscribedUsers = users.Count(u => u.HasSubscription);

            var currentSubscriptions = countInMonth(startOfMonth, users.Where(u => u.HasSubscription));
            var lastMonthSubscriptions = countLastMonth(startOfLastMonth, users.Where(u => u.HasSubscription));
            var subscriptionGrowthRate = lastMonthSubscriptions == 0 ? (currentSubscriptions > 0 ? 100 : 0) : (double)(currentSubscriptions - lastMonthSubscriptions) / lastMonthSubscriptions * 100;

            // implement vehicle growth rate and ticket growth rate as well
            var currentVehicles = countInMonth(startOfMonth, vehicles);
            var lastMonthVehicles = countLastMonth(startOfLastMonth, vehicles);
            var vehicleGrowthRate = lastMonthVehicles == 0 ? (currentVehicles > 0 ? 100 : 0) : (double)(currentVehicles - lastMonthVehicles) / lastMonthVehicles * 100;

            var currentTickets = countInMonth(startOfMonth, tickets);
            var lastMonthTickets = countLastMonth(startOfLastMonth, tickets);
            var ticketGrowthRate = lastMonthTickets == 0 ? (currentTickets > 0 ? 100 : 0) : (double)(currentTickets - lastMonthTickets) / lastMonthTickets * 100;
           

            var dashboardSummary = new DashboardSummaryDto
            {
                TotalUsers = users.Count,
                TotalVehicles = vehicles.Count,
                TotalTickets = tickets.Count,
                SubscribedUsers = subscribedUsers,
                UserGrowthRate = Math.Round(userGrowthRate, 1),
                VehicleGrowthRate = Math.Round(vehicleGrowthRate, 1),
                TicketGrowthRate = Math.Round(ticketGrowthRate, 1),
                SubscriptionGrowthRate = Math.Round(subscriptionGrowthRate, 1)
            };

            return GenericResponse<DashboardSummaryDto>.Success("Successfully retrieved dashboard summary", dashboardSummary);
        }
    }
}
