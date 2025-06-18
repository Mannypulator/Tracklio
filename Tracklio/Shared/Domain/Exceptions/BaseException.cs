namespace Tracklio.Shared.Domain.Exceptions;

public abstract class BaseException : Exception
{
    public string? Code { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    protected BaseException(string message) : base(message) { }
    protected BaseException(string message, Exception innerException) : base(message, innerException) { }
    protected BaseException(string message, string? code) : base(message)
    {
        Code = code;
    }
}