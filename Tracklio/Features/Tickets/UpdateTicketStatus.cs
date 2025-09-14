using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Tickets;

public class UpdateTicketStatus : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // add swagger documentation
        endpointRouteBuilder.MapPut("/api/tickets/status",
        async (
            [FromBody] UpdateTicketStatusCommand command,
            [FromServices] IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.ReturnedResponse();
        })
        .WithName("UpdateTicketStatus")
        .WithTags("Tickets")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Update Ticket Status",
            Description = "Update the status of a parking ticket (e.g., Open, In Review, Resolved).",
            OperationId = "UpdateTicketStatus",
        })
        .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
        .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
        .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PoliciesConstant.AdminOnly);
    }

    public record UpdateTicketStatusCommand(Guid TicketId, string Status) : IRequest<GenericResponse<string>>;

    public class UpdateTicketStatusCommandHandler(RepositoryContext context) : IRequestHandler<UpdateTicketStatusCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
        {
            var ticket = await context.ParkingTickets.FindAsync(request.TicketId, cancellationToken);
            if (ticket == null)
            {
                return GenericResponse<string>.Error(404, "Ticket not found");
            }

            ticket.Status = Enum.Parse<TicketStatus>(request.Status);
            ticket.UpdatedAt = DateTime.UtcNow;

            context.ParkingTickets.Update(ticket);

            await context.SaveChangesAsync(cancellationToken);

            return GenericResponse<string>.Success("Ticket status updated successfully", null!);
        }
    }
}
