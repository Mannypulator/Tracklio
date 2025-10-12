using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Tracklio.Shared.Configurations;
using Tracklio.Shared.Services.Email;
using Tracklio.Shared.Services.OAuth;

namespace Tracklio.Shared.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ITemplateService _templateService;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IGoogleOAuthTokenProvider _tokenProvider;

    public EmailService(
        IOptions<SmtpSettings> smtpSettings,
        ITemplateService templateService,
        ILogger<EmailService> logger,
        IConfiguration configuration,
        IGoogleOAuthTokenProvider tokenProvider)
    {
        _smtpSettings = smtpSettings.Value;
        _templateService = templateService;
        _logger = logger;
        _configuration = configuration;
        _tokenProvider = tokenProvider;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {

            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_smtpSettings.DisplayName);
            email.From.Add(new MailboxAddress(_smtpSettings.DisplayName, _smtpSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_smtpSettings.Server, Convert.ToInt32(_smtpSettings.Port), SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);

            // In development, don't throw - just log and continue
            var isDevelopment = _configuration.GetValue<bool>("ApplicationSettings:IsDevelopment");
            if (isDevelopment)
            {
                _logger.LogWarning("📧 [DEV MODE] Email sending failed but continuing. Email would be sent to: {Email}", toEmail);
                return true; // Return true in dev mode to not break the flow
            }

            throw; // Re-throw in production
        }
    }

    public async Task<bool> SendEmailV2Async(string to, string subject, string htmlBody)
    {
        var fromEmail = _smtpSettings.FromEmail ?? throw new InvalidOperationException("Missing Smtp:FromEmail");
        var smtpHost = _smtpSettings.Server ?? throw new InvalidOperationException("Missing Smtp:Host");
        var smtpPort = _smtpSettings.Port ?? throw new InvalidOperationException("Missing Smtp:Port");
        var smtpUser = _smtpSettings.UserName ?? throw new InvalidOperationException("Missing Smtp:Username");
        var smtpPass = _smtpSettings.Password ?? throw new InvalidOperationException("Missing Smtp:Password");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.DisplayName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, int.Parse(smtpPort), SecureSocketOptions.Auto);
        await client.AuthenticateAsync(smtpUser, smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        return true;
    }

    public async Task<bool> SendEmailViaGoogleApiAsync(string toEmail, string subject, string body)
    {
        try
        {
            var credential = await GetUserCredentialAsync(_smtpSettings.UserName);
            var gmailService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Gmail API .NET"
            });

            var message = CreateMimeMessage(toEmail, subject, body);
            var gmailMessage = new Message
            {
                Raw = Base64UrlEncode(message.ToString())
            };

            var result = await gmailService.Users.Messages.Send(gmailMessage, _smtpSettings.UserName).ExecuteAsync();
            _logger.LogInformation("Email sent successfully via Google API to {Email} with subject: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return false;
        }
    }



    public async Task<bool> SendEmailViaGoogleOAuthAsync(string toEmail, string subject, string body)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMTP_FROM_NAME"))
            ? "Tracklio Support"
            : Environment.GetEnvironmentVariable("SMTP_FROM_NAME"), Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? throw new InvalidOperationException("SMTP_USERNAME not set")));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        msg.Body = bodyBuilder.ToMessageBody();

        var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT") != null ? int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")!) : 587;

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(Environment.GetEnvironmentVariable("SMTP_SERVER"), smtpPort, SecureSocketOptions.StartTls, cancellationToken: CancellationToken.None);

        var accessToken = await _tokenProvider.GetAccessTokenAsync(CancellationToken.None);
        var oauth2 = new MailKit.Security.SaslMechanismOAuth2(Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? throw new InvalidOperationException("SMTP_USERNAME not set"), accessToken);
        await smtp.AuthenticateAsync(oauth2, CancellationToken.None);

        await smtp.SendAsync(msg, CancellationToken.None);
        await smtp.DisconnectAsync(true, CancellationToken.None);

        return true;
    }

    public async Task<bool> SendTemplatedEmailAsync<T>(string toEmail, string subject, string templateName, T model) where T : class
    {
        try
        {
            var htmlBody = await _templateService.RenderTemplateAsync(templateName, model);
            return await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render or send templated email '{TemplateName}' to {Email}", templateName, toEmail);

            // In development, don't throw - just log and continue
            var isDevelopment = _configuration.GetValue<bool>("ApplicationSettings:IsDevelopment");
            if (isDevelopment)
            {
                _logger.LogWarning("📧 [DEV MODE] Templated email failed but continuing. Template: {TemplateName}, To: {Email}", templateName, toEmail);
                return true;
            }

            throw;
        }
    }

    private async Task<UserCredential> GetUserCredentialAsync(string userId)
    {
        var clientSecrets = new ClientSecrets
        {
            ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"),
            ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
        };

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailSend },
            userId,
            CancellationToken.None
        );

        return credential;
    }

    private MimeMessage CreateMimeMessage(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender Name", "sender@gmail.com"));
        message.To.Add(new MailboxAddress(_smtpSettings.DisplayName, to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (body != null)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private string Base64UrlEncode(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(inputBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
}
