using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Services.MOT;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.MOT;

public class GetVehicleHistoryByVin : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v2/mot/history/vin/{vin}",
            async (
                    [FromRoute] string vin,
                    [FromServices] IMediator mediator,
                    CancellationToken ct
                ) =>
            {
                var response = await mediator.Send(new GetVehicleHistoryByVinQuery(vin), ct);
                return response.ReturnedResponse();
            })
                   
            .WithName("GetVehicleMotHistoryByVin")
            .WithTags("MOT")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Get vehicle MOT history by Vin ",
                Description = "Retrieve MOT history for a vehicle by vin.",
                OperationId = "GetVehicleMotHistoryByVin"
            })
            .Produces<GenericResponse<VehicleMotHistory>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
    
    
    public record GetVehicleHistoryByVinQuery(string Vin):IRequest<GenericResponse<VehicleMotHistory>>;
    
    public class GetVehicleHistoryByVinQueryHandler(
            IMotHistoryApiClient motHistoryApiClient, 
            IMotTokenApiClient motTokenApiClient,
            MotConfiguration config,
            ILogger<MotHistoryHandler> logger
        ) 
        : IRequestHandler<GetVehicleHistoryByVinQuery, GenericResponse<VehicleMotHistory>>
    {
        public async Task<GenericResponse<VehicleMotHistory>> Handle(GetVehicleHistoryByVinQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Vin))
            {
                return GenericResponse<VehicleMotHistory>.Error(400, "Vin is required");
            }
            var tokenResponse = await motTokenApiClient.GetTokenAsync
                (config.TenantId, 
                    new TokenRequest(
                    config.GrantType, 
                    config.ClientId, 
                    config.ClientSecret,
                    config.Scope
                    ),
                    cancellationToken
                );
            
            logger.LogInformation($"Token reponse: {tokenResponse}");

            if (!tokenResponse.IsSuccessful)
            {
                return GenericResponse<VehicleMotHistory>.Error(400, tokenResponse.Error.Message);
            }
            
            var motHistoryResponse = await motHistoryApiClient.GetVehicleHistoryByVinAsync(request.Vin, tokenResponse.Content.AccessToken, cancellationToken);
            
            logger.LogInformation($"motHistoryResponse: {motHistoryResponse}");

            if (!motHistoryResponse.IsSuccessful)
            {
                return GenericResponse<VehicleMotHistory>.Error(400, motHistoryResponse.Error.Message);
            }
            
            return GenericResponse<VehicleMotHistory>.Success("Success", motHistoryResponse.Content);
        }
    }
}