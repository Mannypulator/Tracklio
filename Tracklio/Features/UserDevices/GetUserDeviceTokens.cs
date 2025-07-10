using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.UserDevices;

public class GetUserDeviceTokens : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/devices/{userId}", async (
                string userId,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(new GetUserDeviceTokensQuery(userId), ct);
                return response.ReturnedResponse();
            })
            .WithName("GetUserDeviceTokens")
            .WithTags("Device")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get user device tokens",
                Description =
                    "Get user device tokens to tracklio from firebase.",
                OperationId = "GetUserDeviceTokens"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status401Unauthorized)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record  GetUserDeviceTokensQuery(string UserId):  IRequest<GenericResponse<IEnumerable<string>>>;
    
    public class GetUserDeviceTokensHandler(RepositoryContext context, ILogger<GetUserDeviceTokensHandler> logger): IRequestHandler<GetUserDeviceTokensQuery, GenericResponse<IEnumerable<string>>>
    {
        public async Task<GenericResponse<IEnumerable<string>>> Handle(GetUserDeviceTokensQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return GenericResponse<IEnumerable<string>>.Error(400, "Invalid user id");
            }
            var userTokens = await context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .Select(d => d.DeviceToken)
                .ToListAsync(cancellationToken: cancellationToken);
            
            logger.LogInformation($"Retrieved {userTokens.Count} device tokens for user {request.UserId}");
            
            
            return GenericResponse<IEnumerable<string>>.Success("User device tokens gotten",  userTokens);
            
            
        }
    }
}