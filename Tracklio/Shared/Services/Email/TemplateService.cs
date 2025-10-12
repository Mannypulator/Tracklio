using Razor.Templating.Core;

namespace Tracklio.Shared.Services.Email;

public class TemplateService : ITemplateService
{
    public async Task<string> RenderTemplateAsync<T>(string templateName, T model) where T : class
    {
        var templatePath = $"~/EmailTemplates/Views/{templateName}.cshtml";
        var html = await RazorTemplateEngine.RenderAsync(templatePath, model);
        return html;
    }
}
