namespace Tracklio.Shared.Services.Email;

public interface ITemplateService
{
    Task<string> RenderTemplateAsync<T>(string templateName, T model) where T : class;
}
