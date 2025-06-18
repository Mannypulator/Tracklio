namespace Tracklio.Shared.Domain.Exceptions;

public class NotFoundException : BaseException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, string? code) : base(message, code) { }
    public NotFoundException(string resourceType, object id) 
        : base($"{resourceType} with ID '{id}' was not found", $"{resourceType.ToUpperInvariant()}_NOT_FOUND") { }
}

public class ForbiddenException : BaseException
{
    public ForbiddenException(string message) : base(message, "ACCESS_FORBIDDEN") { }
    public ForbiddenException(string message, string? code) : base(message, code) { }
}
public class BusinessRuleException : BaseException
{
    public BusinessRuleException(string message) : base(message, "BUSINESS_RULE_VIOLATION") { }
    public BusinessRuleException(string message, string? code) : base(message, code) { }
}
public class ExternalServiceException : BaseException
{
    public string? ServiceName { get; set; }
    public int? StatusCode { get; set; }

    public ExternalServiceException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
        Code = $"{serviceName.ToUpperInvariant()}_SERVICE_ERROR";
    }

    public ExternalServiceException(string serviceName, string message, int statusCode) : base(message)
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
        Code = $"{serviceName.ToUpperInvariant()}_SERVICE_ERROR";
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ServiceName = serviceName;
        Code = $"{serviceName.ToUpperInvariant()}_SERVICE_ERROR";
    }
}

public class RateLimitException : BaseException
{
    public TimeSpan? RetryAfter { get; set; }

    public RateLimitException(string message) : base(message, "RATE_LIMIT_EXCEEDED") { }
    public RateLimitException(string message, TimeSpan retryAfter) : base(message, "RATE_LIMIT_EXCEEDED")
    {
        RetryAfter = retryAfter;
    }
}