using System.Text.Json.Serialization;
using Refit;

namespace Tracklio.Shared.Services.MOT;

public interface IMotTokenApiClient
{
    [Post("/{tenantId}/oauth2/v2.0/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<ApiResponse<TokenResponse>> GetTokenAsync(
        string tenantId,
        [Body(BodySerializationMethod.UrlEncoded)] TokenRequest request,
        CancellationToken cancellationToken = default
    );
    
    
}

public record TokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("client_secret")] string ClientSecret,
    [property: JsonPropertyName("scope")] string Scope
);


public record TokenResponse(
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("ext_expires_in")] int ExtExpiresIn,
    [property: JsonPropertyName("access_token")] string AccessToken
);

