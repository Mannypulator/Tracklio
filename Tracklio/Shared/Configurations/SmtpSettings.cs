namespace Tracklio.Shared.Configurations;

public class SmtpSettings
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string DisplayName { get; set; }
    public string Server { get; set; }
    public string Port { get; set; }
    public string FromEmail { get; set; }
}