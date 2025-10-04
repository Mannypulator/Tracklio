using System.Text.Json.Serialization;
using Refit;

namespace Tracklio.Shared.Services.MOT;

public interface IMotHistoryApiClient
{
    [Get("/v1/trade/vehicles/registration/{registration}")]
    [Headers("Accept: application/json")]
    Task<ApiResponse<VehicleMotHistory>> GetVehicleHistoryAsync(
        string registration,
        [Header("Authorization")] string authorization,
        [Header("X-Api-Key")] string apiKey,
        CancellationToken cancellationToken = default
    );
    
    [Get("/v1/trade/vehicles/vin/{vin}")]
    [Headers("Accept: application/json")]
    Task<ApiResponse<VehicleMotHistory>> GetVehicleHistoryByVinAsync(
        string vin,
        [Header("Authorization")] string authorization,
        [Header("X-Api-Key")] string apiKey,
        CancellationToken cancellationToken = default
    );
    
    
    [Get("/v1/trade/vehicles/bulk-download")]
    [Headers("Accept: application/json")]
    Task<ApiResponse<BulkDownloadResponse>> GetBulkDownloadLinksAsync(
        [Header("Authorization")] string authorization,
        [Header("X-API-Key")] string apiKey,
        CancellationToken cancellationToken = default
    );

    [Put("/v1/trade/credentials")]
    [Headers("Accept: application/json", "Content-Type: application/json")]
    Task<ApiResponse<UpdateCredentialsResponse>> UpdateCredentialsAsync(
        [Body] UpdateCredentialsRequest request,
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken = default
    );
}

public record MotApiError(
    [property: JsonPropertyName("errorCode")] string ErrorCode,
    [property: JsonPropertyName("errorMessage")] string ErrorMessage,
    [property: JsonPropertyName("requestId")] string RequestId
);

public record MotDefect(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("dangerous")] bool? Dangerous = null
);

public record MotTest(
    [property: JsonPropertyName("registrationAtTimeOfTest")] string? RegistrationAtTimeOfTest,
    [property: JsonPropertyName("completedDate")] DateTime CompletedDate,
    [property: JsonPropertyName("testResult")] string TestResult,
    // CRITICAL: expiryDate is null for failed/abandoned tests
    [property: JsonPropertyName("expiryDate")] DateOnly? ExpiryDate,
    [property: JsonPropertyName("odometerValue")] string? OdometerValue,
    [property: JsonPropertyName("odometerUnit")] string? OdometerUnit,
    [property: JsonPropertyName("odometerResultType")] string? OdometerResultType,
    [property: JsonPropertyName("motTestNumber")] string MotTestNumber,
    [property: JsonPropertyName("dataSource")] string? DataSource,
    [property: JsonPropertyName("location")] string? Location = null,
    [property: JsonPropertyName("defects")] MotDefect[]? Defects = null
);

public record VehicleMotHistory(
    [property: JsonPropertyName("registration")] string Registration,
    [property: JsonPropertyName("make")] string Make,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("firstUsedDate")] DateOnly FirstUsedDate,
    [property: JsonPropertyName("fuelType")] string FuelType,
    [property: JsonPropertyName("primaryColour")] string PrimaryColour,
    [property: JsonPropertyName("registrationDate")] DateOnly RegistrationDate,
    [property: JsonPropertyName("manufactureDate")] DateOnly ManufactureDate,
    [property: JsonPropertyName("engineSize")] string? EngineSize,
    [property: JsonPropertyName("hasOutstandingRecall")] string? HasOutstandingRecall,
    [property: JsonPropertyName("motTests")] MotTest[] MotTests
);

public record BulkDownloadFile(
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("downloadUrl")] string DownloadUrl,
    [property: JsonPropertyName("fileSize")] long FileSize,
    [property: JsonPropertyName("fileCreatedOn")] DateOnly FileCreatedOn
);

public record BulkDownloadResponse(
    [property: JsonPropertyName("bulk")] BulkDownloadFile[] Bulk,
    [property: JsonPropertyName("delta")] BulkDownloadFile[] Delta
);

// Credentials management DTOs
public record UpdateCredentialsRequest(
    [property: JsonPropertyName("awsApiKeyValue")] string AwsApiKeyValue,
    [property: JsonPropertyName("email")] string Email
);

public record UpdateCredentialsResponse(
    [property: JsonPropertyName("clientSecret")] string ClientSecret
);

public class MotConfiguration
{
    public string TenantId { get; init; } = Environment.GetEnvironmentVariable("MOT_TOKEN_TENANT_ID") ?? throw new InvalidOperationException("MOT_TOKEN_TENANT_ID not configured");
    public string GrantType { get; init; } = Environment.GetEnvironmentVariable("MOT_GRANT_TYPE") ?? "client_credentials";
    public string ClientId { get; init; } = Environment.GetEnvironmentVariable("MOT_CLIENT_ID") ?? throw new InvalidOperationException("MOT_CLIENT_ID not configured");
    public string ClientSecret { get; init; } = Environment.GetEnvironmentVariable("MOT_CLIENT_SECRET") ?? throw new InvalidOperationException("MOT_CLIENT_SECRET not configured");
    public string Scope { get; init; } = Environment.GetEnvironmentVariable("MOT_SCOPE") ?? throw new InvalidOperationException("MOT_SCOPE not configured");
    public string ApiKey { get; init; } = Environment.GetEnvironmentVariable("MOT_API_KEY") ?? throw new InvalidOperationException("MOT_API_KEY not configured");
}
