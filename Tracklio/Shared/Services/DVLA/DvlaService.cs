using System.Text.Json;
using Tracklio.Shared.Domain.Exceptions;
using Tracklio.Shared.Networking;
using Tracklio.Shared.Networking.Requests;

namespace Tracklio.Shared.Services.DVLA;

public class DvlaService(IHttpService httpService) : IDvlaService
{
    public async Task<VehicleDetails> GetVehicleDetailsAsync(
        string registrationNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            throw new ArgumentException("Registration number cannot be null or empty", nameof(registrationNumber));
        }

        var baseUrl = Environment.GetEnvironmentVariable("DVLA_BASE_URL")
            ?? throw new InvalidOperationException("DVLA_BASE_URL not configured");
        var apiKey = Environment.GetEnvironmentVariable("DVLA_API_KEY")
            ?? throw new InvalidOperationException("DVLA_API_KEY not configured");

        var url = $"{baseUrl}/vehicle-enquiry/v1/vehicles";

        var requestBody = new VehicleEnquiryRequest(registrationNumber.ToUpperInvariant());
        var request = new PostRequest<VehicleEnquiryRequest>(url, requestBody);

        var headers = new Dictionary<string, string>
        {
            ["x-api-key"] = apiKey,
            ["Content-Type"] = "application/json"
        };

        try
        {
            return await httpService.SendPostRequest<VehicleDetails, VehicleEnquiryRequest>(
                request,
                headers
            );
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Parse the DVLA error response
            var errorMatch = System.Text.RegularExpressions.Regex.Match(
                ex.Message,
                @"Response: ({.*})");

            if (errorMatch.Success)
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<VehicleEnquiryError>(
                        errorMatch.Groups[1].Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    throw new DvlaVehicleNotFoundException(
                        registrationNumber,
                        errorResponse?.Status,
                        errorResponse?.Code);
                }
                catch (JsonException)
                {
                    throw new DvlaVehicleNotFoundException(registrationNumber, null, null);
                }
            }

            throw new DvlaVehicleNotFoundException(registrationNumber, null, null);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // Parse the DVLA error response for bad requests
            var errorMatch = System.Text.RegularExpressions.Regex.Match(
                ex.Message,
                @"Response: ({.*})");

            if (errorMatch.Success)
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<VehicleEnquiryError>(
                        errorMatch.Groups[1].Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    throw new DvlaApiException(
                        errorResponse?.Detail ?? "Invalid request to DVLA API",
                        errorResponse?.Status,
                        errorResponse?.Code,
                        400);
                }
                catch (JsonException)
                {
                    throw new DvlaApiException("Invalid request to DVLA API", null, null, 400);
                }
            }

            throw new DvlaApiException("Invalid request to DVLA API", null, null, 400);
        }
    }
}