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

public class GetUserTicket : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/api/v1/tickets/user/{userId}/ticket", async (
            Guid userId,
            [FromServices] IMediator mediator) =>
        {
            var query = new GetUserTicketQuery(userId);
            var result = await mediator.Send(query);
            return result.ReturnedResponse();
        })
        .WithName("GetUserTicket")
        .WithTags("Tickets")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get a user's ticket",
            Description = "Allows users to retrieve their ticket information.",
            OperationId = "GetUserTicket",
        })
        .Produces<GenericResponse<TicketDto>>(StatusCodes.Status200OK)
        .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PoliciesConstant.MotoristOnly);
    }


    public record GetUserTicketQuery(Guid UserId) : IRequest<GenericResponse<TicketDto>>;

    public class GetUserTicketQueryHandler(RepositoryContext context) : IRequestHandler<GetUserTicketQuery, GenericResponse<TicketDto>>
    {
        public async Task<GenericResponse<TicketDto>> Handle(GetUserTicketQuery request, CancellationToken cancellationToken)
        {
            var ticket = await context.ParkingTickets
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                        .ThenInclude(v => v.User)
                    .FirstOrDefaultAsync(t => t.Vehicle.User.Id == request.UserId, cancellationToken);

            if (ticket == null)
            {
                return GenericResponse<TicketDto>.Error(404, "Ticket not found for the user");
            }

            var ticketDto = new TicketDto
            {
                Id = ticket.Id,
                UserId = ticket.Vehicle.User.Id,
                IssuedDate = ticket.IssuedDate,
                Amount = ticket.Amount,
                Status = ticket.Status.ToString(),
                ProfileImage = ticket.Vehicle.User.ProfileImage,
                VRM = ticket.Vehicle.VRM,
                VehicleMakeModel = ticket.Vehicle.Model
            };

            return GenericResponse<TicketDto>.Success("Success", ticketDto);
        }
    }
}
