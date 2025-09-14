using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Admin;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Admin;

public class GetUsers : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/users", async (
                [FromServices] IMediator mediator,
                [FromBody] GetUsersForAdminQuery query,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(query, ct);
                return response.ReturnedResponse();
            })
            .WithName("GetUsersForAdmin")
            .WithTags("Users")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get Users for Admin",
                Description =
                    "Get all users with pagination. SortOrder can be 'asc' or 'desc'. SortField can be 'FirstName', 'LastName', 'Email', 'CreatedAt'.",
                OperationId = "GetUsersForAdmin",
            })
            .Produces<GenericResponse<PaginatedResult<UserDto>>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization("AdminPolicy");
    }

    public record GetUsersForAdminQuery(
        string? Search,
        int Page = 1,
        int PageSize = 10,
        string SortField = "FirstName",
        string SortOrder = "asc"
    ) : IRequest<GenericResponse<PaginatedResult<UserDto>>>;

    public class GetUsersForAdminQueryHandler(RepositoryContext context) : IRequestHandler<GetUsersForAdminQuery, GenericResponse<PaginatedResult<UserDto>>>
    {
        public async Task<GenericResponse<PaginatedResult<UserDto>>> Handle(GetUsersForAdminQuery request, CancellationToken cancellationToken)
        {
            var query = context.Users.AsNoTracking().AsQueryable();

            if(!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(u => u.FirstName.ToLower().Contains(searchLower) ||
                                         u.LastName.ToLower().Contains(searchLower) ||
                                         u.Email.ToLower().Contains(searchLower) ||
                                         u.Vehicles.Any(v => v.VRM.ToLower().Contains(searchLower))
                                    );
            }

            //sorting
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

            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query
            .Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(),
                HasSubscription = u.HasSubscription,
                IsActive = u.IsActive,
                ProfileImage = u.ProfileImage,
                VehicleCount = u.Vehicles.Count(v => v.IsActive),
                ActivePcnCount = u.Vehicles.Sum(v => v.ParkingTickets.Count(t =>
                    t.Status == Shared.Domain.Enums.TicketStatus.Active))
            })
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return GenericResponse<PaginatedResult<UserDto>>.Success("Successfully retrieved users.", new PaginatedResult<UserDto>
            {
                Data = users,
                Pagination = new PaginationInfo
                {
                    CurrentPage = request.Page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    TotalCount = totalCount,
                    PageSize = request.PageSize
                }
            });
        }
    }


}
