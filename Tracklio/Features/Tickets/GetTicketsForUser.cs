using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Tickets;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Tickets;

public class GetTicketsForUser : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/api/v1/tickets/user/{userId}/all", async (
            Guid userId,
            [FromServices] IMediator mediator) =>
        {
            var query = new GetTicketsForUserQuery(userId);
            var result = await mediator.Send(query);
            return result.ReturnedResponse();
        })
        .WithName("GetTicketsForUser")
        .WithTags("Tickets")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get tickets for a user",
            Description = "Allows users to retrieve their tickets information.",
            OperationId = "GetTicketsForUser",
        })
        .Produces<GenericResponse<IReadOnlyList<TicketDto>>>(StatusCodes.Status200OK)
        .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PoliciesConstant.MotoristOnly);
    }

    public record GetTicketsForUserQuery(Guid UserId) : IRequest<GenericResponse<IReadOnlyList<TicketDto>>>;


    public class GetTicketsForUserQueryHandler(RepositoryContext context) : IRequestHandler<GetTicketsForUserQuery, GenericResponse<IReadOnlyList<TicketDto>>>
    {
        public async Task<GenericResponse<IReadOnlyList<TicketDto>>> Handle(GetTicketsForUserQuery request, CancellationToken cancellationToken)
        {
            var tickets = await context.ParkingTickets
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                        .ThenInclude(v => v.User)
                    .Where(t => t.Vehicle.User.Id == request.UserId)
                    .Select(t => new TicketDto
                    {
                        Id = t.Id,
                        UserId = t.Vehicle.User.Id,
                        IssuedDate = t.IssuedDate,
                        Amount = t.Amount,
                        Status = t.Status.ToString(),
                        ProfileImage = t.Vehicle.User.ProfileImage,
                        VRM = t.Vehicle.VRM,
                        VehicleMakeModel = t.Vehicle.Model
                    })
                    .ToListAsync(cancellationToken);

            if (tickets == null || tickets.Count == 0)
            {
                return GenericResponse<IReadOnlyList<TicketDto>>.Error(404, "No tickets found for the user");
            }

            return GenericResponse<IReadOnlyList<TicketDto>>.Success("Success", tickets);
        }
    }

}
