namespace Tracklio.EmailTemplates.Models;

public class NewPcnIssuedModel
{
    public string FirstName { get; set; } = string.Empty;
    public string VehicleRegistration { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TicketUrl { get; set; } = "#";
    public string SupportEmail { get; set; } = "xxx@email.com";
    public string SupportPhone { get; set; } = "555-555-555";
}
