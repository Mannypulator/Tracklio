using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
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

public class GetUserVehicle : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/vehicles{vehicleId}", async (
                [FromRoute] string vehicleId,
                ClaimsPrincipal claims,
                [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                var request = new GetVehicleQuery();
                request.UserId = userId;
                request.VehicleId = vehicleId;
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("GetUserVehicle")
            .WithTags("Vehicles")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Endpoint to get a vehicle",
                Description =
                    "Allows user to get a vehicle.",
                OperationId = "GetUserVehicle",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PoliciesConstant.MotoristOrAdmin);
    }

    public class GetVehicleQuery : IRequest<GenericResponse<VehicleResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; set; }
        public string? VehicleId { get; set; }
    }

    public class GetVehicleQueryValidator : AbstractValidator<GetVehicleQuery>
    {
        public GetVehicleQueryValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().NotNull();
            RuleFor(x => x.VehicleId).NotEmpty().NotNull();
        }
    }

    public class GetVehicleQueryHandler(RepositoryContext context) : IRequestHandler<GetVehicleQuery, GenericResponse<VehicleResponse>>
    {
        public async  Task<GenericResponse<VehicleResponse?>> Handle(GetVehicleQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return GenericResponse<VehicleResponse>.Error(401, "Unauthorized");
            }

            if (!Guid.TryParse(request.VehicleId, out var vehicleId))
            {
                return GenericResponse<VehicleResponse>.Error(400, "Enter a valid vehicle id");
            }
            
            var vehicle = await context
                .Vehicles
                .AsNoTracking()
                .Include(x => x.ParkingTickets)
                .FirstOrDefaultAsync( x=> x.UserId == request.UserId && x.Id == vehicleId && x.IsActive, cancellationToken);

            if (vehicle is null)
            {
                return GenericResponse<VehicleResponse>.Error(404, "Vehicle not found");
            }
            var vehicleResponse = vehicle.MapToDto();
            
            return GenericResponse<VehicleResponse>.Success("Vehcile gotten successfully", vehicleResponse);
        }
    }
}