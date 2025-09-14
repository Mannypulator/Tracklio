using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracklio.Shared.Domain.Entities;

public class UserSubscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; } // null = active

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Cancelled, Expired

    [Required]
    [MaxLength(10)]
    public string BillingPeriod { get; set; } = "monthly"; // monthly, yearly

    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountPaid { get; set; }

    [MaxLength(100)]
    public string PaymentMethod { get; set; } = "Stripe";

    [MaxLength(100)]
    public string ExternalSubscriptionId { get; set; } = string.Empty; // Stripe sub ID

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual SubscriptionPlan Plan { get; set; } = null!;
}