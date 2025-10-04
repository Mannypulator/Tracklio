using System;
using Tracklio.Shared.Networking;
using Tracklio.Shared.Networking.Requests;

namespace Tracklio.Shared.Services.MOT;

public class MotService(IHttpService httpService) : IMotService
{
    private static TokenResponse? _cachedToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);
    public Task<BulkDownloadResponse> GetBulkDownloadLinksAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check if we have a valid cached token
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        // Use semaphore to prevent multiple simultaneous token requests
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            // Build the token endpoint URL
            var tenantId = Environment.GetEnvironmentVariable("MOT_TOKEN_TENANT_ID")
                ?? throw new InvalidOperationException("MOT_TOKEN_TENANT_ID not configured");
            var baseUrl = Environment.GetEnvironmentVariable("MOT_TOKEN_BASE_URL")
                ?? "https://login.microsoftonline.com";

            var tokenUrl = $"{baseUrl}/{tenantId}/oauth2/v2.0/token";

            // Prepare the form data
            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = Environment.GetEnvironmentVariable("MOT_GRANT_TYPE") ?? "client_credentials",
                ["client_id"] = Environment.GetEnvironmentVariable("MOT_CLIENT_ID")
                    ?? throw new InvalidOperationException("MOT_CLIENT_ID not configured"),
                ["client_secret"] = Environment.GetEnvironmentVariable("MOT_CLIENT_SECRET")
                    ?? throw new InvalidOperationException("MOT_CLIENT_SECRET not configured"),
                ["scope"] = Environment.GetEnvironmentVariable("MOT_SCOPE") ?? "https://tapi.dvsa.gov.uk/.default"
            };

            var request = new PostRequest<Dictionary<string, string>>(tokenUrl, formData);

            // Set the required headers for OAuth2 token request
            var headers = new Dictionary<string, string>
            {
            };

            var tokenResponse = await httpService.SendPostRequest<TokenResponse, Dictionary<string, string>>(
                request,
                headers
            );

            // Cache the token with a buffer (expire 5 minutes early to be safe)
            _cachedToken = tokenResponse;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);

            return tokenResponse;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<VehicleMotHistory> GetVehicleHistoryByRegistrationAsync(
         string registration,
         CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registration))
        {
            throw new ArgumentException("Registration cannot be null or empty", nameof(registration));
        }

        // Get the access token (will use cached token if valid)
        var tokenResponse = await GetTokenAsync(cancellationToken);

        // Get configuration
        var baseUrl = Environment.GetEnvironmentVariable("MOT_HISTORY_BASE_URL")
            ?? "https://history.mot.api.gov.uk";
        var apiKey = Environment.GetEnvironmentVariable("MOT_HISTORY_API_KEY")
            ?? throw new InvalidOperationException("MOT_HISTORY_API_KEY not configured");

        // Build the request URL - uppercase the registration as per MOT API standards
        var url = $"{baseUrl}/v1/trade/vehicles/registration/{registration.ToUpperInvariant()}";
        var request = new GetRequest(url);

        // Set required headers
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {tokenResponse.AccessToken}",
            ["X-API-Key"] = apiKey,
            ["Accept"] = "application/json"
        };

        return await httpService.SendGetRequest<VehicleMotHistory>(request, headers);
    }

    public async Task<VehicleMotHistory> GetVehicleHistoryByVinAsync(
    string vin,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vin))
        {
            throw new ArgumentException("VIN cannot be null or empty", nameof(vin));
        }

        // Get the access token (will use cached token if valid)
        var tokenResponse = await GetTokenAsync(cancellationToken);

        // Get configuration
        var baseUrl = Environment.GetEnvironmentVariable("MOT_HISTORY_BASE_URL")
            ?? "https://history.mot.api.gov.uk";
        var apiKey = Environment.GetEnvironmentVariable("MOT_HISTORY_API_KEY")
            ?? throw new InvalidOperationException("MOT_HISTORY_API_KEY not configured");

        // Build the request URL - uppercase the VIN as per MOT API standards
        var url = $"{baseUrl}/v1/trade/vehicles/vin/{vin.ToUpperInvariant()}";
        var request = new GetRequest(url);

        // Set required headers
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {tokenResponse.AccessToken}",
            ["X-API-Key"] = apiKey,
            ["Accept"] = "application/json"
        };

        return await httpService.SendGetRequest<VehicleMotHistory>(request, headers);
    }
}
