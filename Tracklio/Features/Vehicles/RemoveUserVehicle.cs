using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Vehicle;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Mappings;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Vehicles;

public class RemoveUserVehicle : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapDelete("api/v1/vehicles{vehicleId}", async (
                [FromRoute] string vehicleId,
                ClaimsPrincipal claims,
               [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                var request = new RemoveUserVehicleCommand(userId, vehicleId);
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("RemoveUserVehicle")
            .WithTags("Vehicles")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Endpoint to remove a vehicle",
                Description =
                    "Allows user to remove a vehicle.",
                OperationId = "RemoveUserVehicle",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PoliciesConstant.MotoristOrAdmin);
    }

    public class RemoveUserVehicleCommand(Guid userId, string vehicleId) : IRequest<GenericResponse<string>>
    {
        public Guid UserId { get; set; } = userId;
        public string VehicleId { get; set; } = vehicleId;
    }

    public class RemoveUserVehicleCommandHandler(RepositoryContext context) : IRequestHandler<RemoveUserVehicleCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string?>> Handle(RemoveUserVehicleCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return GenericResponse<string>.Error(401, "Unauthorized");
            }

            if (!Guid.TryParse(request.VehicleId, out var vehicleId))
            {
                return GenericResponse<string>.Error(400, "Enter a valid vehicle id");
            }
            
            var vehicle = await context
                .Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync( x=> x.UserId == request.UserId && x.Id == vehicleId && x.IsActive, cancellationToken);

            if (vehicle is null)
            {
                return GenericResponse<string>.Error(404, "Vehicle not found");
            }

            context.Vehicles.Remove(vehicle);
            
            await context.SaveChangesAsync(cancellationToken);
            
            return GenericResponse<string>.Success("Vehicle removed", vehicle.Id.ToString());
        }
    }
    
    
}