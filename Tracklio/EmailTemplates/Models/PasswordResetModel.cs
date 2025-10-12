namespace Tracklio.EmailTemplates.Models;

public class PasswordResetModel
{
    public string FirstName { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "xxx@email.com";
    public string SupportPhone { get; set; } = "555-555-555";
}
