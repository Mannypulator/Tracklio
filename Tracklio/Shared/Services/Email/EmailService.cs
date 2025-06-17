using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Tracklio.Shared.Configurations;

namespace Tracklio.Shared.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(IOptions<SmtpSettings> smtpSettings)
    {
        _smtpSettings = smtpSettings.Value;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        var email = new MimeMessage();
        email.Sender = MailboxAddress.Parse(_smtpSettings.DisplayName);
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        var builder = new BodyBuilder();
        builder.HtmlBody = body;
        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        smtp.CheckCertificateRevocation = false;
        await smtp.ConnectAsync(_smtpSettings.Server, Convert.ToInt32(_smtpSettings.Port), SecureSocketOptions.SslOnConnect);
        await smtp.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
        return true;

    }
}