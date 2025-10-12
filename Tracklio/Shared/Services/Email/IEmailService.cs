namespace Tracklio.Shared.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
    Task<bool> SendEmailV2Async(string to, string subject, string htmlBody);

    Task<bool> SendEmailViaGoogleOAuthAsync(string toEmail, string subject, string body);
    Task<bool> SendEmailViaGoogleApiAsync(string toEmail, string subject, string body);
    Task<bool> SendTemplatedEmailAsync<T>(string toEmail, string subject, string templateName, T model) where T : class;
}