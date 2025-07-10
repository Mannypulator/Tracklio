namespace Tracklio.Shared.Domain.Dto.Notification;

public class NotificationResponse
{
    public bool Success { get; set; }
    public string MessageId { get; set; }
    public string Error { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedTokens { get; set; } = [];
}