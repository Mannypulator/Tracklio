using System.ComponentModel.DataAnnotations;

namespace Tracklio.Shared.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string VRM { get; set; } = string.Empty; 

    [MaxLength(50)]
    public string? Make { get; set; }

    [MaxLength(50)]
    public string? Model { get; set; }

    [MaxLength(30)]
    public string? Color { get; set; }

    public int? Year { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public DateTime? LastSyncAt { get; set; }
    
    public virtual User User { get; set; } = null!;
    public virtual ICollection<ParkingTicket> ParkingTickets { get; set; } = new List<ParkingTicket>();
}
