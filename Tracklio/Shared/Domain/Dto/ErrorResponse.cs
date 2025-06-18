namespace Tracklio.Shared.Domain.Dto;

public class ErrorResponse
{
    public bool Success { get; set; } = false;
    public int StatusCode { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public object? Errors { get; set; }
    public string? Details { get; set; }
    public string? SupportMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
}