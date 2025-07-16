using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.UserDevices;

public class RemoveDeviceToken : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/devices/remove-device-token", async (
                RemoveDeviceTokenCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("RemoveDeviceToken")
            .WithTags("Device")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Remove device token",
                Description =
                    "Remove device token to tracklio from firebase.",
                OperationId = "RemoveDeviceToken"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status401Unauthorized)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record  RemoveDeviceTokenCommand(string DeviceToken): IRequest<GenericResponse<string>>;
    
    public class RemoveDeviceTokenCommandHandler(
        RepositoryContext context, 
        ILogger<RemoveDeviceTokenCommandHandler> logger):  IRequestHandler<RemoveDeviceTokenCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(RemoveDeviceTokenCommand request, CancellationToken cancellationToken)
        {
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken, cancellationToken: cancellationToken);

            if (device is null)
            {
                return GenericResponse<string>.Error(404, "Device token not found");
            }

            context.UserDevices.Remove(device);
            await context.SaveChangesAsync(cancellationToken);
                    
            logger.LogInformation($"Device token removed for user {device.UserId}");
                
            return GenericResponse<string>.Success("Device token removed", null!);

        }
    }
}