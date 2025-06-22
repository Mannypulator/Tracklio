using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Vehicle;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Mappings;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Vehicles;

public sealed class GetUserVehicles : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/vehicles", async (
                ClaimsPrincipal claims,
                [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                var request = new GetUserVehiclesQuery
                {
                    UserId = userId
                };
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("GetUserVehicles")
            .WithTags("Vehicles")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Endpoint to get a user vehicles",
                Description =
                    "Allows user to get all vehicles.",
                OperationId = "GetUserVehicles",
            })
            .Produces<GenericResponse<IEnumerable<VehicleResponse>>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<IEnumerable<VehicleResponse>>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PoliciesConstant.MotoristOrAdmin);
    }

    public class GetUserVehiclesQuery :  IRequest<GenericResponse<IEnumerable<VehicleResponse>>>
    {
        public Guid UserId { get; init; }
    }

    public class GetUserVehiclesQueryHandler(RepositoryContext context) : IRequestHandler<GetUserVehiclesQuery, GenericResponse<IEnumerable<VehicleResponse>>>
    {
        public async Task<GenericResponse<IEnumerable<VehicleResponse>>> Handle(GetUserVehiclesQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return GenericResponse<IEnumerable<VehicleResponse>>.Error(400, "UserId should not be empty");
            }
            
            var vehicles = await context
                .Vehicles
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId)
                .ToListAsync(cancellationToken);

            var vehicleResponse = vehicles.MapToDto();
            
            return GenericResponse<IEnumerable<VehicleResponse>>.Success("Successfully gotten vehicles", vehicleResponse);
        }
    }
}