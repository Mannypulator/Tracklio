using Tracklio.Shared.Slices;

namespace Tracklio.Features.Health;

public class HealthCheck : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
    }
}