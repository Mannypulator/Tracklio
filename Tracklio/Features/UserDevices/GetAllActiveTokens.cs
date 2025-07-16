using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.UserDevices;

public class GetAllActiveTokens : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/devices", async (
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(new GetAllActiveTokensQuery(), ct);
                return response.ReturnedResponse();
            })
            .WithName("GetAllActiveTokens")
            .WithTags("Device")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get All active user device tokens",
                Description =
                    "Get all active user device tokens to tracklio from firebase.",
                OperationId = "GetAllActiveTokens"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status401Unauthorized)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record GetAllActiveTokensQuery(): IRequest<GenericResponse<IEnumerable<string>>>;
    
    public class GetAllActiveTokensHandler(
        RepositoryContext context, 
        ILogger<GetAllActiveTokensHandler> logger) : IRequestHandler<GetAllActiveTokensQuery, GenericResponse<IEnumerable<string>>>
    {
        public async Task<GenericResponse<IEnumerable<string>>> Handle(GetAllActiveTokensQuery request, CancellationToken cancellationToken)
        {
            var activeTokens = await context.UserDevices
                .Where(d => d.IsActive)
                .Select(d => d.DeviceToken)
                .ToListAsync(cancellationToken: cancellationToken);
            
            logger.LogInformation($"Retrieved {activeTokens.Count} active device tokens");
            
            return GenericResponse<IEnumerable<string>>.Success("Active device tokens retrieved", activeTokens);
        }
    }
}