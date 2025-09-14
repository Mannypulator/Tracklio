using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Tickets;
using Tracklio.Shared.Domain.Dto.Vehicle;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Tickets;

public class GetTicketDetails : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // complete the route check previous examples
        endpointRouteBuilder.MapGet("api/v1/tickets/{TicketId}", async (
               [FromServices] IMediator mediator,
                [FromRoute] Guid TicketId,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(new GetTicketDetailsQuery(TicketId), ct);
                return response.ReturnedResponse();
            })
            .WithName("GetTicketDetails")
            .WithTags("Tickets")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get Ticket Details",
                Description =
                    "Get detailed information about a specific parking ticket, including images and actions taken.",
                OperationId = "GetTicketDetails",
            })
            .Produces<GenericResponse<TicketDetailsDto>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    public record GetTicketDetailsQuery(Guid TicketId) : IRequest<GenericResponse<TicketDetailsDto>>;

    public class GetTicketDetailsQueryHandler(RepositoryContext context) : IRequestHandler<GetTicketDetailsQuery, GenericResponse<TicketDetailsDto>>
    {
        public async Task<GenericResponse<TicketDetailsDto>> Handle(GetTicketDetailsQuery request, CancellationToken cancellationToken)
        {
            var ticket = await context.ParkingTickets
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                        .ThenInclude(v => v.User)
                    .Include(t => t.Actions)
                    .Include(t => t.Images)
                    .FirstOrDefaultAsync(t => t.Id == request.TicketId);

            if (ticket == null)
            {
                return GenericResponse<TicketDetailsDto>.Error(404, "Ticket not found");
            }

            var images = ticket.Images.Select(i => i.Url).ToList();

            var actions = ticket.Actions.Select(a => new TicketActionDto
            {
                Id = a.Id,
                Type = nameof(a.ActionType),
                Description = a.Notes,
                ActionDate = a.ActionDate,
                User = $"{ticket.Vehicle.User.FirstName}{ticket.Vehicle.User.LastName}" ?? "System"
            }).ToList();

            var result = new TicketDetailsDto
            {
                Ticket = new ParkingTicketDto
                {
                    Id = ticket.Id,
                    PCNReference = ticket.PCNReference,
                    VRM = ticket.VRM,
                    IssuedDate = ticket.IssuedDate,
                    Location = ticket.Location,
                    Reason = ticket.Reason,
                    Amount = ticket.Amount,
                    DiscountedAmount = ticket.DiscountedAmount,
                    PaymentDeadline = ticket.PaymentDeadline,
                    AppealDeadline = ticket.AppealDeadline,
                    Status = ticket.Status.ToString(),
                    IssuingAuthority = ticket.IssuingAuthority,
                    PaymentUrl = ticket.PaymentUrl,
                    AppealUrl = ticket.AppealUrl,
                    DataProvider = ticket.DataProvider
                },
                Images = images,
                Actions = actions
            };

            return GenericResponse<TicketDetailsDto>.Success("Success", result);

        }
    }
}
