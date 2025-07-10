using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public class GoogleSignOn : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/api/auth/google-login", (HttpContext http) =>
                Results.Challenge(new AuthenticationProperties
                {
                    RedirectUri = "/api/auth/google-callback"
                }, [GoogleDefaults.AuthenticationScheme]))
            .WithTags("Auth");
    }
}