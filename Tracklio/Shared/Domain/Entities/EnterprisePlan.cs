using System;

namespace Tracklio.Shared.Domain.Entities;

public class EnterprisePlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public int VehiclesAllowed { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PlanPrice { get; set; }
    public decimal DurationPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
