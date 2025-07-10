using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Domain.Dto.Notification;

public class SendNotificationRequest
{
    public string Title { get; set; }
    public string Body { get; set; }
    public string DeviceToken { get; set; }
    public string ClickAction { get; set; }
    public string? ImageUrl { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

public class BulkNotificationRequest
{
    public string Title { get; set; }
    public string Body { get; set; }
    public List<string> DeviceTokens { get; set; }
    public string ImageUrl { get; set; }
}

public class TopicSubscriptionRequest
{
    public List<string> DeviceTokens { get; set; }
    public string Topic { get; set; }
}


public class DeviceTokenRequest
{
    public string UserId { get; set; }
    public string DeviceToken { get; set; }
    public string Platform { get; set; } 
}
