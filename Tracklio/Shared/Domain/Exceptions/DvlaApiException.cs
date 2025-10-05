using System;

namespace Tracklio.Shared.Domain.Exceptions;

public class DvlaApiException : Exception
{
    public string? Code { get; }
    public string? Status { get; }
    public int StatusCode { get; }

    public DvlaApiException(string message, string? status, string? code, int statusCode)
        : base(message)
    {
        Status = status;
        Code = code;
        StatusCode = statusCode;
    }
}

public class DvlaVehicleNotFoundException : DvlaApiException
{
    public string RegistrationNumber { get; }

    public DvlaVehicleNotFoundException(string registrationNumber, string? status, string? code)
        : base($"Vehicle with registration '{registrationNumber}' not found in DVLA database", status, code, 404)
    {
        RegistrationNumber = registrationNumber;
    }
}
