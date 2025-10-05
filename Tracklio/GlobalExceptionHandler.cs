using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Exceptions;

namespace Tracklio;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var response = httpContext.Response;
        response.ContentType = "application/json";

        var correlationId = Guid.NewGuid().ToString();

        // Handle different exception types and get appropriate response
        var genericResponse = HandleSpecificException(exception, correlationId);

        // Set HTTP status code
        response.StatusCode = genericResponse.StatusCode;

        // Log the exception
        LogException(exception, httpContext, correlationId, genericResponse.StatusCode);

        // Write response
        await WriteResponseAsync(response, genericResponse, cancellationToken);

        return true; // Exception was handled
    }

    private GenericResponse<object> HandleSpecificException(Exception exception, string correlationId)
    {
        return exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            NotFoundException notFoundEx => HandleNotFoundException(notFoundEx),
            UnauthorizedAccessException => HandleUnauthorizedException(),
            ForbiddenException forbiddenEx => HandleForbiddenException(forbiddenEx),
            BusinessRuleException businessEx => HandleBusinessRuleException(businessEx),
            DbUpdateException dbEx => HandleDatabaseException(dbEx, correlationId),
            TimeoutException timeoutEx => HandleTimeoutException(timeoutEx, correlationId),
            ArgumentException argumentEx => HandleArgumentException(argumentEx),
            InvalidOperationException invalidOpEx => HandleInvalidOperationException(invalidOpEx),
            ExternalServiceException externalEx => HandleExternalServiceException(externalEx, correlationId),
            RateLimitException rateLimitEx => HandleRateLimitException(rateLimitEx),
            HttpRequestException httpEx => HandleHttpRequestException(httpEx, correlationId),
            TaskCanceledException taskCancelledEx => HandleTaskCancelledException(taskCancelledEx),
            OperationCanceledException => HandleOperationCancelledException(),
            JsonException jsonEx => HandleJsonException(jsonEx),
            NotSupportedException => HandleNotSupportedException(),
            DvlaApiException dvlaEx => HandleDvlaApiException(dvlaEx),
            _ => HandleGenericException(exception, correlationId)
        };
    }

    private GenericResponse<object> HandleValidationException(ValidationException ex)
    {
        var validationErrors = ex.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => ToCamelCase(g.Key),
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var errorData = new
        {
            type = "ValidationError",
            errors = validationErrors
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "Validation failed",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleDvlaApiException(DvlaApiException ex)
    {
        var errorData = new
        {
            type = "DvlaApiError",
            code = ex.Code,
            status = ex.Status,
            statusCode = ex.StatusCode
        };

        return new GenericResponse<object>
        {
            StatusCode = ex.StatusCode >= 400 && ex.StatusCode < 600 ? ex.StatusCode : (int)HttpStatusCode.BadGateway,
            Message = "An error occurred while communicating with the DVLA service.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleNotFoundException(NotFoundException ex)
    {
        var errorData = new
        {
            type = "NotFound",
            code = ex.Code
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.NotFound,
            Message = ex.Message,
            Data = errorData
        };
    }

    private GenericResponse<object> HandleUnauthorizedException()
    {
        var errorData = new
        {
            type = "Unauthorized",
            code = "AUTHENTICATION_REQUIRED"
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Message = "Authentication required",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleForbiddenException(ForbiddenException ex)
    {
        var errorData = new
        {
            type = "Forbidden",
            code = ex.Code
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.Forbidden,
            Message = ex.Message,
            Data = errorData
        };
    }

    private GenericResponse<object> HandleBusinessRuleException(BusinessRuleException ex)
    {
        var errorData = new
        {
            type = "BusinessRuleViolation",
            code = ex.Code
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = ex.Message,
            Data = errorData
        };
    }

    private GenericResponse<object> HandleDatabaseException(DbUpdateException ex, string correlationId)
    {
        var innerException = ex.InnerException?.Message ?? string.Empty;

        if (IsUniqueConstraintViolation(innerException))
        {
            var errorData = new
            {
                type = "DuplicateRecord",
                code = "DUPLICATE_RECORD"
            };

            return new GenericResponse<object>
            {
                StatusCode = (int)HttpStatusCode.Conflict,
                Message = "A record with this information already exists",
                Data = errorData
            };
        }

        if (IsForeignKeyViolation(innerException))
        {
            var errorData = new
            {
                type = "ReferentialIntegrity",
                code = "REFERENTIAL_INTEGRITY_VIOLATION"
            };

            return new GenericResponse<object>
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Cannot perform this operation due to related data constraints",
                Data = errorData
            };
        }

        if (IsConnectionError(innerException))
        {
            var errorData = new
            {
                type = "DatabaseUnavailable",
                code = "DATABASE_UNAVAILABLE",
                correlationId
            };

            return new GenericResponse<object>
            {
                StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                Message = "Database is temporarily unavailable. Please try again later.",
                Data = errorData
            };
        }

        // Generic database error
        var genericErrorData = new
        {
            type = "DatabaseError",
            code = "DATABASE_ERROR",
            correlationId
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = "A database error occurred. Please try again later.",
            Data = genericErrorData
        };
    }

    private GenericResponse<object> HandleTimeoutException(TimeoutException ex, string correlationId)
    {
        var errorData = new
        {
            type = "Timeout",
            code = "REQUEST_TIMEOUT",
            correlationId
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.RequestTimeout,
            Message = "The request timed out. Please try again.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleArgumentException(ArgumentException ex)
    {
        var errorData = new
        {
            type = "InvalidArgument",
            code = "INVALID_ARGUMENT"
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = ex.Message,
            Data = errorData
        };
    }

    private GenericResponse<object> HandleInvalidOperationException(InvalidOperationException ex)
    {
        var errorData = new
        {
            type = "InvalidOperation",
            code = "INVALID_OPERATION"
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = ex.Message,
            Data = errorData
        };
    }

    private GenericResponse<object> HandleExternalServiceException(ExternalServiceException ex, string correlationId)
    {
        var errorData = new
        {
            type = "ExternalServiceError",
            code = ex.Code,
            serviceName = ex.ServiceName,
            correlationId
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadGateway,
            Message = $"External service '{ex.ServiceName}' is currently unavailable. Please try again later.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleRateLimitException(RateLimitException ex)
    {
        var errorData = new
        {
            type = "RateLimit",
            code = "RATE_LIMIT_EXCEEDED",
            retryAfterSeconds = ex.RetryAfter?.TotalSeconds
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.TooManyRequests,
            Message = "Rate limit exceeded. Please slow down your requests.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleHttpRequestException(HttpRequestException ex, string correlationId)
    {
        var errorData = new
        {
            type = "ExternalServiceError",
            code = "HTTP_REQUEST_FAILED",
            correlationId
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadGateway,
            Message = "An external service request failed. Please try again later.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleTaskCancelledException(TaskCanceledException ex)
    {
        if (ex.InnerException is TimeoutException)
        {
            var timeoutData = new
            {
                type = "Timeout",
                code = "REQUEST_TIMEOUT"
            };

            return new GenericResponse<object>
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Message = "The request timed out. Please try again.",
                Data = timeoutData
            };
        }

        var errorData = new
        {
            type = "RequestCancelled",
            code = "REQUEST_CANCELLED"
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "The request was cancelled.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleOperationCancelledException()
    {
        var errorData = new
        {
            type = "OperationCancelled",
            code = "OPERATION_CANCELLED"
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "The operation was cancelled.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleJsonException(JsonException ex)
    {
        var errorData = new
        {
            type = "InvalidJson",
            code = "INVALID_JSON",
            details = _environment.IsDevelopment() ? ex.Message : null
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "Invalid JSON format in request body.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleNotSupportedException()
    {
        var errorData = new
        {
            type = "NotSupported",
            code = "OPERATION_NOT_SUPPORTED"
        };

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "The requested operation is not supported.",
            Data = errorData
        };
    }

    private GenericResponse<object> HandleGenericException(Exception ex, string correlationId)
    {
        var errorData = new
        {
            type = "InternalServerError",
            code = "INTERNAL_SERVER_ERROR",
            correlationId,
            details = _environment.IsDevelopment() ? ex.Message : null,
            stackTrace = _environment.IsDevelopment() ? ex.StackTrace : null
        };

        var message = _environment.IsDevelopment()
            ? ex.Message
            : "An unexpected error occurred. Please try again later.";

        return new GenericResponse<object>
        {
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = message,
            Data = errorData
        };
    }

    private async Task WriteResponseAsync(HttpResponse response, GenericResponse<object> genericResponse,
        CancellationToken cancellationToken)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(genericResponse, jsonOptions);
        await response.WriteAsync(jsonResponse, cancellationToken);
    }

    private void LogException(Exception exception, HttpContext context, string correlationId, int statusCode)
    {
        var logLevel = GetLogLevel(statusCode);
        var userId = GetUserId(context);
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
        var ipAddress = GetClientIpAddress(context);

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId,
            ["StatusCode"] = statusCode,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path,
            ["QueryString"] = context.Request.QueryString.ToString(),
            ["UserAgent"] = userAgent ?? "Unknown",
            ["IpAddress"] = ipAddress,
            ["ExceptionType"] = exception.GetType().Name
        });

        _logger.Log(logLevel, exception,
            "Exception occurred: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message);

        // Log additional context for critical errors
        if (statusCode >= 500)
        {
            _logger.LogError(
                "Critical error details - User: {UserId}, IP: {IpAddress}, Path: {Path}, Exception: {Exception}",
                userId, ipAddress, context.Request.Path, exception.ToString());
        }
    }

    private LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }

    private string GetUserId(HttpContext context)
    {
        return context.User?.FindFirst("sub")?.Value
               ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? "Anonymous";
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    private bool IsUniqueConstraintViolation(string message)
    {
        var uniqueViolationKeywords = new[]
        {
            "duplicate key", "unique constraint", "violates unique",
            "already exists", "duplicate entry", "unique index"
        };

        return uniqueViolationKeywords.Any(keyword =>
            message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsForeignKeyViolation(string message)
    {
        var foreignKeyKeywords = new[]
        {
            "foreign key", "reference constraint", "violates foreign key",
            "referential integrity", "foreign key constraint"
        };

        return foreignKeyKeywords.Any(keyword =>
            message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsConnectionError(string message)
    {
        var connectionKeywords = new[]
        {
            "connection", "timeout", "network", "host", "server",
            "unreachable", "refused", "unavailable"
        };

        return connectionKeywords.Any(keyword =>
            message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}