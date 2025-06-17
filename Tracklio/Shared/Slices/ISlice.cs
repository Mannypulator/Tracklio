using System;

namespace Tracklio.Shared.Slices;

public interface ISlice
{
    void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder);
}
