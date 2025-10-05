using System.Text.Json.Serialization;
using Refit;

namespace Tracklio.Shared.Services.DVLA;

public interface IDvlaApiClient
{
    [Post("/vehicle-enquiry/v1/vehicles")]
    [Headers("Content-Type: application/json")]
    Task<ApiResponse<VehicleDetails>> GetVehicleDetailsAsync(
        [Body] VehicleEnquiryRequest request,
        CancellationToken cancellationToken = default
    );
}


public record VehicleEnquiryRequest(
    [property: JsonPropertyName("registrationNumber")] string RegistrationNumber
);

// Response DTO as record
public record VehicleDetails(
    [property: JsonPropertyName("artEndDate")] DateOnly? ArtEndDate,
    [property: JsonPropertyName("co2Emissions")] int? Co2Emissions,
    [property: JsonPropertyName("colour")] string Colour,
    [property: JsonPropertyName("engineCapacity")] int? EngineCapacity,
    [property: JsonPropertyName("fuelType")] string FuelType,
    [property: JsonPropertyName("make")] string Make,
    [property: JsonPropertyName("markedForExport")] bool MarkedForExport,
    [property: JsonPropertyName("monthOfFirstRegistration")] string MonthOfFirstRegistration,
    [property: JsonPropertyName("motStatus")] string MotStatus,
    [property: JsonPropertyName("registrationNumber")] string RegistrationNumber,
    [property: JsonPropertyName("revenueWeight")] int? RevenueWeight,
    [property: JsonPropertyName("taxDueDate")] DateOnly? TaxDueDate,
    [property: JsonPropertyName("taxStatus")] string TaxStatus,
    [property: JsonPropertyName("typeApproval")] string? TypeApproval,
    [property: JsonPropertyName("wheelplan")] string? Wheelplan,
    [property: JsonPropertyName("yearOfManufacture")] int? YearOfManufacture,
    [property: JsonPropertyName("euroStatus")] string? EuroStatus,
    [property: JsonPropertyName("realDrivingEmissions")] string? RealDrivingEmissions,
    [property: JsonPropertyName("dateOfLastV5CIssued")] DateOnly? DateOfLastV5CIssued
);

// Error response DTOs
public record DvlaApiErrorResponse(
    [property: JsonPropertyName("errors")] VehicleEnquiryError[] Errors
);

public record VehicleEnquiryError(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail
);