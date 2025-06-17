using System;
using System.ComponentModel.DataAnnotations;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Domain.Entities;

public class SyncLog
{
    public Guid Id { get; set; }

    [MaxLength(10)]
    public string? VRM { get; set; }

    public Guid? VehicleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DataProvider { get; set; } = string.Empty;

    public SyncStatus Status { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public int TicketsFound { get; set; } = 0;

    public int TicketsProcessed { get; set; } = 0;

    public int TicketsCreated { get; set; } = 0;

    public int TicketsUpdated { get; set; } = 0;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public TimeSpan? Duration => CompletedAt - StartedAt;

    // Navigation properties
    public virtual Vehicle? Vehicle { get; set; }
}
