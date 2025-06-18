namespace Tracklio.Shared.Domain.Enums;

public enum TicketActionType
{
    Created = 0,
    PaymentInitiated = 1,
    PaymentCompleted = 2,
    AppealSubmitted = 3,
    AppealAccepted = 4,
    AppealRejected = 5,
    StatusUpdated = 6,
    NotificationSent = 7
}
