using PuppeteerSharp;
using PuppeteerSharp.Media;
using Tracklio.Shared.Services.Email;

namespace Tracklio.Shared.Services.Pdf;

public class PdfService : IPdfService
{
    private readonly ITemplateService _templateService;
    private static bool _browserFetched = false;
    private static readonly SemaphoreSlim _browserFetchLock = new(1, 1);

    public PdfService(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    private async Task EnsureBrowserDownloadedAsync()
    {
        if (_browserFetched) return;

        await _browserFetchLock.WaitAsync();
        try
        {
            if (!_browserFetched)
            {
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
                _browserFetched = true;
            }
        }
        finally
        {
            _browserFetchLock.Release();
        }
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string html)
    {
        await EnsureBrowserDownloadedAsync();

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        });

        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);

        var pdfData = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "20px",
                Right = "20px",
                Bottom = "20px",
                Left = "20px"
            }
        });

        return pdfData;
    }

    public async Task<byte[]> GeneratePdfFromTemplateAsync<T>(string templateName, T model) where T : class
    {
        var html = await _templateService.RenderTemplateAsync(templateName, model);
        return await GeneratePdfFromHtmlAsync(html);
    }
}
