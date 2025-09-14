using System.ComponentModel.DataAnnotations;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Motorist;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public bool HasSubscription { get; set; } = false;
    public string? ProfileImage { get; set; }

    public bool EmailConfirmed { get; set; } = false;

    public bool PhoneNumberConfirmed { get; set; } = false;

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public virtual NotificationPreferences? NotificationPreferences { get; set; }
    public virtual ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();

    public virtual ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();

    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();

    // Add this collection
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
