using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Tickets;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Tickets;

public class GetUserTickets : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // complete this method to add the endpoint to your API
        endpointRouteBuilder.MapGet("api/v1/tickets", async(
                [FromServices] IMediator mediator,
                [AsParameters] GetUserTicketQuery query,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(query, ct);
                return response.ReturnedResponse();
            })
            .WithName("GetUserTickets")
            .WithTags("Tickets")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get User Tickets",
                Description =
                    "Get all parking tickets for the authenticated user with pagination. SortOrder can be 'asc' or 'desc'. SortField can be 'IssuedDate', 'Amount', 'UserName'.",
                OperationId = "GetUserTickets",
            })
            .Produces<GenericResponse<PaginatedResult<TicketDto>>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();

    }

    public record GetUserTicketQuery
    (
        string? Search,
        DateTime? StartDate,
        DateTime? EndDate,
        string SortField = "IssuedDate",
        string SortOrder = "desc",
        int Page = 1,
        int PageSize = 10
    ) : IRequest<GenericResponse<PaginatedResult<TicketDto>>>;


    public class GetUserTicketQueryHandler(RepositoryContext context) : IRequestHandler<GetUserTicketQuery, GenericResponse<PaginatedResult<TicketDto>>>
    {
        public async Task<GenericResponse<PaginatedResult<TicketDto>>> Handle(GetUserTicketQuery request, CancellationToken cancellationToken)
        {
            var search = string.Empty;
            var query = context.ParkingTickets
                        .Include(t => t.Vehicle)
                            .ThenInclude(v => v.User)
                        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                search = search.ToLower();
                query = query.Where(t =>
                    t.Vehicle.User.FirstName.ToLower().Contains(search) ||
                    t.Vehicle.User.LastName.ToLower().Contains(search) ||
                    t.Vehicle.User.Email.ToLower().Contains(search) ||
                    t.VRM.ToLower().Contains(search) ||
                    t.Vehicle.Make.ToLower().Contains(search) ||
                    t.Vehicle.Model.ToLower().Contains(search) ||
                    t.Vehicle.Year.ToString().Contains(search)
                );
            }

            if (request.EndDate.HasValue)
                query = query.Where(t => t.IssuedDate <= request.EndDate.Value);

            // Sort
            query = request.SortField switch
            {
                "UserName" => request.SortOrder == "desc" ? query.OrderByDescending(t => t.Vehicle.User.FirstName) : query.OrderBy(t => t.Vehicle.User.FirstName),
                "IssuedDate" => request.SortOrder == "desc" ? query.OrderByDescending(t => t.IssuedDate) : query.OrderBy(t => t.IssuedDate),
                "Amount" => request.SortOrder == "desc" ? query.OrderByDescending(t => t.Amount) : query.OrderBy(t => t.Amount),
                _ => request.SortOrder == "desc" ? query.OrderByDescending(t => t.IssuedDate) : query.OrderBy(t => t.IssuedDate)
            };


            var ticketDtos = await query.Select(t => new TicketDto
            {
                Id = t.Id,
                UserId = t.Vehicle.UserId,
                UserName = $"{t.Vehicle.User.FirstName} {t.Vehicle.User.LastName}",
                AccountReference = t.ExternalTicketId ?? "N/A",
                VRM = t.VRM,
                VehicleMakeModel = $"{t.Vehicle.Make} {t.Vehicle.Model}",
                Amount = t.Amount,
                IssuedDate = t.IssuedDate,
                Status = t.Status.ToString(),
                ProfileImage = "/images/profile.jpg"
            }).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync();


            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var result = new PaginatedResult<TicketDto>
            {
                Data = ticketDtos,
                Pagination = new PaginationInfo
                {
                    CurrentPage = request.Page,
                    TotalPages = totalPages,
                    TotalCount = totalCount,
                    PageSize = request.PageSize
                }
            };

            return GenericResponse<PaginatedResult<TicketDto>>.Success("Success", result);

        }
    }
}
