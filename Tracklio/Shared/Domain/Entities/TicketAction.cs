using System.ComponentModel.DataAnnotations;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Domain.Entities;

public class TicketAction
{
    public Guid Id { get; set; }

    [Required]
    public Guid TicketId { get; set; }

    public TicketActionType ActionType { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? ExternalReference { get; set; }

    public bool IsSuccessful { get; set; } = true;

    // Navigation properties
    public virtual ParkingTicket Ticket { get; set; } = null!;
}
