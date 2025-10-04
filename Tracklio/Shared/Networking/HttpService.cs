using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tracklio.Shared.Networking.Requests;

namespace Tracklio.Shared.Networking;

public class HttpService : IHttpService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T> SendPostRequest<T, U>(
    PostRequest<U> request,
    Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient("Tracklio");
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, request.Url);

        // Add request headers (not content headers)
        foreach (var header in headers.Where(h => h.Key != "Content-Type"))
        {
            httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Check if data is Dictionary<string, string> for form-urlencoded
        if (request.Data is Dictionary<string, string> formData)
        {
            // Send as form-urlencoded (required for OAuth2 token endpoints)
            httpRequestMessage.Content = new FormUrlEncodedContent(formData);
        }
        else if (request.Data != null)
        {
            // Send as JSON for other requests
            httpRequestMessage.Content = JsonContent.Create(request.Data);
        }

        var response = await client.SendAsync(httpRequestMessage);

        // Read response content for debugging
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Request failed with status {response.StatusCode}. Response: {responseContent}");
        }

        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<T> SendGetRequest<T>(
      GetRequest request,
      Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient("Tracklio");
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, request.Url);

        foreach (var header in headers)
        {
            httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await client.SendAsync(httpRequestMessage);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Request to {request.Url} failed with status {response.StatusCode}. Response: {responseContent}");
        }

        var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            // Handle null values gracefully
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        });

        return result ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task<T> SendMultipartPostRequest<T>(
        MultipartPostRequest request,
        Dictionary<string, string> headers)
    {
        var client = _httpClientFactory.CreateClient("Tracklio");
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, request.Url);

        // Add headers
        foreach (var header in headers)
        {
            httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var multipartContent = new MultipartFormDataContent();

        // Add form fields
        foreach (var field in request.FormFields)
        {
            multipartContent.Add(new StringContent(field.Value.ToString()), field.Key);
        }

        // Add files
        foreach (var file in request.FileFields)
        {
            var fileContent = new StreamContent(file.Value.OpenReadStream());
            multipartContent.Add(fileContent, file.Key, file.Value.FileName);
        }

        httpRequestMessage.Content = multipartContent;

        var response = await client.SendAsync(httpRequestMessage);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}