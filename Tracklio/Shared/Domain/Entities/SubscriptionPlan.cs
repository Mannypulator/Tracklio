using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracklio.Shared.Domain.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // e.g., "Solo", "Family"

    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty; // e.g., "Solo plan"

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Icon { get; set; } = string.Empty; // e.g., "⚡", "➕"

    [Column(TypeName = "decimal(10,2)")]
    public decimal PriceMonthly { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PriceYearly { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "GBP"; 

    public int MaxVehicles { get; set; }

    public bool IsPopular { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
}
