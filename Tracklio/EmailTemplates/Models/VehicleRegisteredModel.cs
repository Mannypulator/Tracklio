namespace Tracklio.EmailTemplates.Models;

public class VehicleRegisteredModel
{
    public string FirstName { get; set; } = string.Empty;
    public string VehicleRegistration { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "xxx@email.com";
    public string SupportPhone { get; set; } = "555-555-555";
}
