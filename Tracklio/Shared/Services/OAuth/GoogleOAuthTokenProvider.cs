using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

namespace Tracklio.Shared.Services.OAuth;

public sealed class GoogleOAuthTokenProvider : IGoogleOAuthTokenProvider
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Reuse token if valid for >60s
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _expiresAt.AddSeconds(-60))
            return _accessToken!;

        await _gate.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _expiresAt.AddSeconds(-60))
                return _accessToken!;

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID not set"),
                    ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? throw new InvalidOperationException("GOOGLE_CLIENT_SECRET not set")
                }
            });
            
            var refreshToken = Environment.GetEnvironmentVariable("GOOGLE_SMTP_REFRESH_TOKEN");

            var token = new TokenResponse { RefreshToken = refreshToken ?? throw new InvalidOperationException("GOOGLE_REFRESH_TOKEN not set") };
            var cred = new UserCredential(flow, "smtp-user", token);

            var ok = await cred.RefreshTokenAsync(ct);
            if (!ok || string.IsNullOrEmpty(cred.Token?.AccessToken))
                throw new InvalidOperationException("Failed to refresh Gmail access token. Verify ClientId/Secret/RefreshToken.");

            _accessToken = cred.Token.AccessToken!;
            // Default to 55 minutes if no expiry; Gmail tokens typically ~3600s
            _expiresAt = (cred.Token.IssuedUtc != default(DateTime) && cred.Token.ExpiresInSeconds.HasValue)
                ? new DateTimeOffset(cred.Token.IssuedUtc).AddSeconds(cred.Token.ExpiresInSeconds.Value)
                : DateTimeOffset.UtcNow.AddMinutes(55);

            return _accessToken!;
        }
        finally
        {
            _gate.Release();
        }
    }
}
