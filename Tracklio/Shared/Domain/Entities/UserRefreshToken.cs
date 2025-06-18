using System.ComponentModel.DataAnnotations;

namespace Tracklio.Shared.Domain.Entities;

public class UserRefreshToken
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedByIp { get; set; }

    [MaxLength(100)]
    public string? RevokedByIp { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
