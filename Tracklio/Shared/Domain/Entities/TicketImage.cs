using System;

namespace Tracklio.Shared.Domain.Entities;

public class TicketImage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ParkingTicket Ticket { get; set; } = null!;
}
