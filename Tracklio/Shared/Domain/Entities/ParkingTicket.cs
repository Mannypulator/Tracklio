using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Domain.Entities;

public class ParkingTicket
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string PCNReference { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string VRM { get; set; } = string.Empty;

    [Required]
    public Guid VehicleId { get; set; }

    public DateTime IssuedDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DiscountedAmount { get; set; }

    public DateTime PaymentDeadline { get; set; }

    public DateTime? AppealDeadline { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Active;

    [Required]
    [MaxLength(100)]
    public string IssuingAuthority { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PaymentUrl { get; set; }

    [MaxLength(500)]
    public string? AppealUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public DateTime? LastNotificationSent { get; set; }

    [MaxLength(50)]
    public string? ExternalTicketId { get; set; }

    [MaxLength(100)]
    public string? DataProvider { get; set; }

    // Navigation properties
    public virtual Vehicle Vehicle { get; set; } = null!;
    public virtual ICollection<TicketAction> Actions { get; set; } = new List<TicketAction>();
}
