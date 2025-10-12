namespace Tracklio.Shared.Services.Pdf;

public interface IPdfService
{
    Task<byte[]> GeneratePdfFromHtmlAsync(string html);
    Task<byte[]> GeneratePdfFromTemplateAsync<T>(string templateName, T model) where T : class;
}
