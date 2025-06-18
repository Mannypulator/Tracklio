using System.ComponentModel.DataAnnotations;

namespace Tracklio.Shared.Domain.Entities;

public class NotificationPreferences
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public bool EmailNotifications { get; set; } = true;

    public bool SmsNotifications { get; set; } = false;

    public bool PushNotifications { get; set; } = true;

    public bool NewTicketNotifications { get; set; } = true;

    public bool PaymentReminderNotifications { get; set; } = true;

    public bool AppealStatusNotifications { get; set; } = true;

    public bool DeadlineReminderNotifications { get; set; } = true;

    public int ReminderDaysBefore { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
