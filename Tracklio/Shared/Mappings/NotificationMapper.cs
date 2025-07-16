using Tracklio.Features.Notifications;
using Tracklio.Shared.Domain.Dto.Notification;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Mappings;

public static class NotificationMapper
{
    public static SendNotificationRequest MapToDto(this SendNotification.SendNotificationCommand dto, string deviceToken)
    {
        return new SendNotificationRequest()
        {
            Body = dto.Body,
            Title = dto.Title,
            DeviceToken = deviceToken,
            Priority = NotificationPriority.High
        };
    }
}