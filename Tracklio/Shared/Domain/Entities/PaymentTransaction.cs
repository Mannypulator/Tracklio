using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracklio.Shared.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string PlanName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty; // e.g., "Successful", "Failed", "Refunded"

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty; // e.g., "Stripe", "PayPal"

    [MaxLength(100)]
    public string? TransactionId { get; set; } // External ID (e.g., Stripe charge ID)

    [MaxLength(500)]
    public string? ReceiptUrl { get; set; } // Link to receipt

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public DateTime RenewalDate { get; set; }

    [MaxLength(10)]
    public string BillingPeriod { get; set; } = "monthly"; // monthly, yearly

    // Navigation
    public virtual User User { get; set; } = null!;
}
