namespace Tracklio.EmailTemplates.Models;

public class AppealRejectedModel
{
    public string FirstName { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "xxx@email.com";
    public string SupportPhone { get; set; } = "555-555-555";
}
