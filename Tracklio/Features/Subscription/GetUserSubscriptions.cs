using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Subscription;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Admin;

public class GetUserSubscriptions : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // complete the route check previous examples
        endpointRouteBuilder.MapGet("/api/v1/subscriptions/user",
        async (
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? sortField,
            [FromQuery] string? sortOrder,
            [FromServices] IMediator mediator) =>
        {
            var query = new GetUserSubscriptionsQuery
            (
                search,
                page ?? 1,
                pageSize ?? 10,
                sortField,
                sortOrder 
            );
            var result = await mediator.Send(query);
            return result.ReturnedResponse();
        })
        .WithName("GetUserSubscriptions")
        .WithTags("Subscriptions")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get User Subscriptions",
            Description = "Retrieve a list of user subscriptions.",
            OperationId = "GetUserSubscriptions",
        })
        .Produces<GenericResponse<PaginatedResult<UserSubscriptionDto>>>(StatusCodes.Status200OK)
        .Produces<GenericResponse<PaginatedResult<UserSubscriptionDto>>>(StatusCodes.Status404NotFound)
        .RequireAuthorization("AdminPolicy");
    }

    public record GetUserSubscriptionsQuery
    (
        string? Search,
       int Page = 1,
       int PageSize = 10,
       string SortField = "Email",
       string SortOrder = "asc"
    ) : IRequest<GenericResponse<PaginatedResult<UserSubscriptionDto>>>;


    public class GetUserSubscriptionsQueryHandler(RepositoryContext context) : IRequestHandler<GetUserSubscriptionsQuery, GenericResponse<PaginatedResult<UserSubscriptionDto>>>
{
        public async Task<GenericResponse<PaginatedResult<UserSubscriptionDto>>> Handle(GetUserSubscriptionsQuery request, CancellationToken cancellationToken)
        {
            var search = string.Empty;
            var query = context.Users
                .AsNoTracking()
                .Include(u => u.Subscriptions)
                    .ThenInclude(us => us.Plan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.Vehicles.Any(v => v.VRM.ToLower().Contains(search))
                );
            }


            query = (request.SortField, request.SortOrder.ToLower()) switch
            {
                ("FirstName", "asc") => query.OrderBy(u => u.FirstName),
                ("FirstName", "desc") => query.OrderByDescending(u => u.FirstName),
                ("LastName", "asc") => query.OrderBy(u => u.LastName),
                ("LastName", "desc") => query.OrderByDescending(u => u.LastName),
                ("Email", "asc") => query.OrderBy(u => u.Email),
                ("Email", "desc") => query.OrderByDescending(u => u.Email),
                ("CreatedAt", "asc") => query.OrderBy(u => u.CreatedAt),
                ("CreatedAt", "desc") => query.OrderByDescending(u => u.CreatedAt),
                _ => query.OrderBy(u => u.FirstName)
            };


            var userDtos = await query.Select(u => new UserSubscriptionDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                CurrentPlan = u.Subscriptions.Where(s => s.EndDate == null).Select(s => s.Plan.Name).FirstOrDefault() ?? "Freemium",
                VehiclesUsed = u.Vehicles.Count(v => v.IsActive),
                VehiclesAllowed = u.Subscriptions.Where(s => s.EndDate == null).Select(s => s.Plan.MaxVehicles).FirstOrDefault(),
                Status = u.Subscriptions.Where(s => s.EndDate == null).Select(s => s.Status).FirstOrDefault() ?? "Inactive",
                RenewalDate = u.Subscriptions.Where(s => s.EndDate == null).Select(s => s.EndDate).FirstOrDefault(),
                PaymentDate = u.Subscriptions.Where(s => s.EndDate == null).Select(s => s.StartDate).FirstOrDefault(),
                ProfileImage = u.ProfileImage
            }).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();


            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var result = new PaginatedResult<UserSubscriptionDto>
            {
                Data = userDtos,
                Pagination = new PaginationInfo
                {
                    CurrentPage = request.Page,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    PageSize = request.PageSize
                }
            };

            return GenericResponse<PaginatedResult<UserSubscriptionDto>>.Success("Success", result);
            
        }
    }
}
