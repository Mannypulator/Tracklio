namespace Tracklio.EmailTemplates.Models;

public class EmailVerificationModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 10;
    public string SupportEmail { get; set; } = "xxx@email.com";
    public string SupportPhone { get; set; } = "555-555-555";
}
