using Tracklio.Shared.Domain.Dto.Notification;

namespace Tracklio.Shared.Services.Notification;

public interface IFirebaseService
{
    Task<NotificationResponse> SendNotificationAsync(SendNotificationRequest request);
    Task<NotificationResponse> SendBulkNotificationAsync(BulkNotificationRequest request);
    Task<NotificationResponse> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string> data = null);
    Task<NotificationResponse> SubscribeToTopicAsync(TopicSubscriptionRequest request);
    Task<NotificationResponse> UnsubscribeFromTopicAsync(TopicSubscriptionRequest request);
    Task<bool> ValidateTokenAsync(string deviceToken);
}