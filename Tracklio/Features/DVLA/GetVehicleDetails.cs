using MediatR;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Services.DVLA;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.DVLA;

public class GetVehicleDetails : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/dvla/registrationNumber", async (
                string registrationNumber,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(new GetVehicleDetailsQuery(registrationNumber), ct);
                return response.ReturnedResponse();
            })
            .WithName("GetVehicleDetails")
            .WithTags("DVLA")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get dvla vehicle details",
                Description =
                    "Get dvla vehicle details for the specified registration number",
                OperationId = "GetVehicleDetails",
            })
            .Produces<GenericResponse<VehicleDetails>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<VehicleDetails>>(StatusCodes.Status400BadRequest);
    }
    
    public record GetVehicleDetailsQuery(string RegistrationNumber): IRequest<GenericResponse<VehicleDetails>>;
    
    public class GetVehicleDetailsQueryHandler(IDvlaService dvlaService) : IRequestHandler<GetVehicleDetailsQuery, GenericResponse<VehicleDetails>>
    {
        public async Task<GenericResponse<VehicleDetails>> Handle(GetVehicleDetailsQuery request, CancellationToken cancellationToken)
        {
            var vehicleDetailsResponse =
                await dvlaService.GetVehicleDetailsAsync(request.RegistrationNumber, cancellationToken);

            if (vehicleDetailsResponse == null)
            {
                return GenericResponse<VehicleDetails>.Error(404, "Vehicle not found");
            }
            
            return GenericResponse<VehicleDetails>.Success("Success", vehicleDetailsResponse);
        }
    }
}