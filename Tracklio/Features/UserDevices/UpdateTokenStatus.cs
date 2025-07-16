using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.UserDevices;

public class UpdateTokenStatus : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/devices/update-token-status", async (
                [FromBody] UpdateTokenStatusCommand request,
                [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("UpdateTokenStatus")
            .WithTags("Device")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Update user device token status",
                Description =
                    "Update user device token status to tracklio from firebase.",
                OperationId = "UpdateTokenStatus"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status401Unauthorized)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record UpdateTokenStatusCommand(string DeviceToken, bool IsActive): IRequest<GenericResponse<string>>;

    public class UpdateTokenStatusCommandHandler(RepositoryContext context, ILogger<UpdateTokenStatusCommandHandler> logger) : IRequestHandler<UpdateTokenStatusCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(UpdateTokenStatusCommand request, CancellationToken cancellationToken)
        {
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken, cancellationToken: cancellationToken);

            if (device == null)
            {
                return GenericResponse<string>.Error(404, "Device token not found");
            }

            device.IsActive = request.IsActive;
            device.UpdatedAt = DateTime.UtcNow;
                
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation($"Updated token status to {request.IsActive} for token ending in ...{request.DeviceToken.Substring(request.DeviceToken.Length - 10)}");
                
            return GenericResponse<string>.Success("Updated token status to active", null!);

        }
    }
}