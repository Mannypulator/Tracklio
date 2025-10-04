namespace Tracklio.Shared.Networking.Requests;

public record GetRequest(string Url);

public record PostRequest<T>(string Url, T? Data);

public class MultipartPostRequest
{
    public string Url { get; set; }
    public Dictionary<string, object> FormFields { get; set; } = new();
    public Dictionary<string, List<object>> MultipleFormFields { get; set; } = new();
    public Dictionary<string, IFormFile> FileFields { get; set; } = new();
}
