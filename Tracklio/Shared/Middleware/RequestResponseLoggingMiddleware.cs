using System.Text;
using Microsoft.IO;

namespace Tracklio.Shared.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await LogRequest(context);
        await LogResponse(context);
    }

    private async Task LogRequest(HttpContext context)
    {
        context.Request.EnableBuffering();

        using var requestStream = _recyclableMemoryStreamManager.GetStream();
        await context.Request.Body.CopyToAsync(requestStream);

        var requestInfo = new
        {
            Schema = context.Request.Scheme,
            Host = context.Request.Host.Value,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            Headers = context.Request.Headers
                .Where(h => !h.Key.Contains("Authorization", StringComparison.OrdinalIgnoreCase)) // Skip sensitive headers
                .ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body = await ReadStreamInChunks(requestStream)
        };

        _logger.LogInformation(
            "HTTP Request Information:{NewLine}" +
            "Schema: {Schema}{NewLine}" +
            "Host: {Host}{NewLine}" +
            "Path: {Path}{NewLine}" +
            "QueryString: {QueryString}{NewLine}" +
            "Headers: {Headers}{NewLine}" +
            "Body: {Body}",
            Environment.NewLine,
            requestInfo.Schema,
            requestInfo.Host,
            requestInfo.Path,
            requestInfo.QueryString,
            System.Text.Json.JsonSerializer.Serialize(requestInfo.Headers),
            requestInfo.Body);

        context.Request.Body.Position = 0;
    }

    private async Task LogResponse(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        using var responseStream = _recyclableMemoryStreamManager.GetStream();
        context.Response.Body = responseStream;

        await _next(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        var responseInfo = new
        {
            StatusCode = context.Response.StatusCode,
            Headers = context.Response.Headers
                .Where(h => !h.Key.Contains("Authorization", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body = responseBody
        };

        _logger.LogInformation(
            "HTTP Response Information:{NewLine}" +
            "StatusCode: {StatusCode}{NewLine}" +
            "Headers: {Headers}{NewLine}" +
            "Body: {Body}",
            Environment.NewLine,
            responseInfo.StatusCode,
            System.Text.Json.JsonSerializer.Serialize(responseInfo.Headers),
            responseInfo.Body);

        await responseStream.CopyToAsync(originalBodyStream);
    }

    private static async Task<string> ReadStreamInChunks(Stream stream)
    {
        stream.Position = 0;
        using var textWriter = new StringWriter();
        using var reader = new StreamReader(stream);

        var chunkSize = 4096;
        var readBuffer = new char[chunkSize];
        int readCount;

        do
        {
            readCount = await reader.ReadBlockAsync(readBuffer, 0, chunkSize);
            await textWriter.WriteAsync(readBuffer, 0, readCount);
        } while (readCount > 0);

        return textWriter.ToString();
    }
}