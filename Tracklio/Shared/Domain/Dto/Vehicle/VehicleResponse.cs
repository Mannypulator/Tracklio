namespace Tracklio.Shared.Domain.Dto.Vehicle;

public class VehicleResponse
{
    public Guid Id { get; set; }
    public string VRM { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public int? Year { get; set; }
    public string DisplayName => $"{Make} {Model}".Trim();
    public Guid UserId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsActive { get; set; }
    public int ActiveTicketCount { get; set; }
    public int TotalTicketCount { get; set; }
    public decimal TotalOutstandingAmount { get; set; }
}