using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Services.MOT;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.MOT;

public class GetVehicleHistory : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v2/mot/history/reg/{registration}", 
                async (
                    [FromRoute] string registration,
                    [FromServices] MotHistoryHandler handler,
                    CancellationToken ct
                ) => await handler.HandleAsync(registration, ct))
            .WithName("GetVehicleMotHistory")
            .WithTags("MOT")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Get vehicle MOT history ",
                Description = "Retrieve MOT history for a vehicle by registration number using improved handler pattern.",
                OperationId = "GetVehicleMotHistory"
            })
            .Produces<GenericResponse<VehicleMotHistory>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}

public class MotHistoryHandler
{
    private readonly IMotTokenApiClient _motTokenApiClient;
    private readonly IMotHistoryApiClient _motHistoryApiClient;
    private readonly ILogger<MotHistoryHandler> _logger;
    private readonly MotConfiguration _config;

    public MotHistoryHandler(
        IMotTokenApiClient motTokenApiClient,
        IMotHistoryApiClient motHistoryApiClient,
        ILogger<MotHistoryHandler> logger,
        MotConfiguration config)
    {
        _motTokenApiClient = motTokenApiClient;
        _motHistoryApiClient = motHistoryApiClient;
        _logger = logger;
        _config = config;
    }

    public async Task<IResult> HandleAsync(string registration, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing MOT history request for registration: {Registration}", registration);

            // Validate registration format
            if (string.IsNullOrWhiteSpace(registration))
            {
                return Results.BadRequest(
                    GenericResponse<string>.Error(400, "Invalid registration number format")
                );
            }

            // Get OAuth token
            var tokenResult = await GetAccessTokenAsync(cancellationToken);
            
            _logger.LogInformation($"Token Result: {tokenResult}");
            
            if (tokenResult.IsFailure)
            {
                return Results.BadRequest(tokenResult.Error);
            }
            
            var historyResult = await GetMotHistoryAsync(registration, tokenResult.AccessToken, cancellationToken);
            
            _logger.LogInformation($"History Result: {historyResult}");
            
            if (historyResult.IsFailure)
            {
                return historyResult.StatusCode == 404 
                    ? Results.NotFound(historyResult.Error)
                    : Results.BadRequest(historyResult.Error);
            }

            _logger.LogInformation("Successfully retrieved MOT history for registration: {Registration}", registration);
            
            return Results.Ok(
                GenericResponse<VehicleMotHistory>.Success(
                    "Vehicle history retrieved successfully", 
                    historyResult.Data
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing MOT history request for registration: {Registration}", registration);
            
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }

    private async Task<TokenResult> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tokenResponse = await _motTokenApiClient.GetTokenAsync(
                _config.TenantId,
                new TokenRequest(_config.GrantType, _config.ClientId, _config.ClientSecret,_config.Scope),
                cancellationToken
            );

            if (!tokenResponse.IsSuccessful)
            {
                var errorMessage = ExtractErrorMessage(tokenResponse.Error?.Content, "Failed to obtain access token");
                return TokenResult.Failure(GenericResponse<string>.Error(500, errorMessage));
            }

            return TokenResult.Success(tokenResponse.Content.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining access token");
            return TokenResult.Failure(GenericResponse<string>.Error(500, "Authentication failed"));
        }
    }

    private async Task<HistoryResult> GetMotHistoryAsync(string registration, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var motHistoryResponse = await _motHistoryApiClient.GetVehicleHistoryAsync(
                registration, 
                accessToken, 
                cancellationToken
            );

            if (!motHistoryResponse.IsSuccessful)
            {
                var statusCode = (int)motHistoryResponse.Error.StatusCode;
                var errorMessage = ExtractErrorMessage(motHistoryResponse.Error?.Content, 
                    $"Vehicle history not found for registration: {registration}");
                
                return HistoryResult.Failure(
                    GenericResponse<string>.Error(statusCode, errorMessage),
                    statusCode
                );
            }

            return HistoryResult.Success(motHistoryResponse.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MOT history for registration: {Registration}", registration);
            return HistoryResult.Failure(GenericResponse<string>.Error(500, "Failed to retrieve vehicle history"), 500);
        }
    }

    private static string ExtractErrorMessage(string? errorContent, string fallbackMessage)
    {
        if (string.IsNullOrEmpty(errorContent))
            return fallbackMessage;

        try
        {
            var errorResponse = JsonSerializer.Deserialize<MotApiError>(errorContent);
            return errorResponse?.ErrorMessage ?? fallbackMessage;
        }
        catch
        {
            return fallbackMessage;
        }
    }

   

    // Result types for better error handling
    private record TokenResult(bool IsFailure, string AccessToken, GenericResponse<string>? Error)
    {
        public static TokenResult Success(string accessToken) => new(false, accessToken, null);
        public static TokenResult Failure(GenericResponse<string> error) => new(true, string.Empty, error);
    }

    private record HistoryResult(bool IsFailure, VehicleMotHistory? Data, GenericResponse<string>? Error, int StatusCode)
    {
        public static HistoryResult Success(VehicleMotHistory data) => new(false, data, null, 200);
        public static HistoryResult Failure(GenericResponse<string> error, int statusCode) => new(true, null, error, statusCode);
    }
}

