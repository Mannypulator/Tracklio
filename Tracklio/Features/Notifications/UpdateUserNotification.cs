// using FluentValidation;
// using MediatR;
// using Tracklio.Shared.Domain.Dto;
// using Tracklio.Shared.Slices;
//
// namespace Tracklio.Features.Notifications;
//
// public class UpdateUserNotification : ISlice
// {
//     public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
//     {
//         throw new NotImplementedException();
//     }
//
//     public class UpdateUserNotificationCommand : IRequest<GenericResponse<string>>
//     {
//         public bool EmailNotifications { get; set; } = true;
//         public bool SmsNotifications { get; set; } = false;
//         public bool PushNotifications { get; set; } = true;
//         public bool NewTicketNotifications { get; set; } = true;
//         public bool PaymentReminderNotifications { get; set; } = true;
//         public bool AppealStatusNotifications { get; set; } = true;
//         public bool DeadlineReminderNotifications { get; set; } = true;
//         public int ReminderDaysBefore { get; set; } = 3; 
//     }
//
//     public class UpdateUserNotificationCommandValidator : AbstractValidator<UpdateUserNotificationCommand>
//     {
//         public UpdateUserNotificationCommandValidator()
//         {
//             RuleFor(x => x.ReminderDaysBefore)
//                 .GreaterThanOrEqualTo(1)
//                 .WithMessage("Reminder days must be at least 1 day")
//                 .LessThanOrEqualTo(30)
//                 .WithMessage("Reminder days must not exceed 30 days");
//
//             RuleFor(x => x)
//                 .Must(x => x.EmailNotifications || x.SmsNotifications || x.PushNotifications)
//                 .WithMessage("At least one notification method (Email, SMS, or Push) must be enabled")
//                 .OverridePropertyName("NotificationMethods");
//
//
//             RuleFor(x => x)
//                 .Must(x =>
//                 {
//                     var hasNotificationTypes = x.NewTicketNotifications || x.PaymentReminderNotifications ||
//                                                x.AppealStatusNotifications || x.DeadlineReminderNotifications;
//
//                     if (!hasNotificationTypes) return true;
//
//                     return x.EmailNotifications || x.SmsNotifications || x.PushNotifications;
//                 })
//                 .WithMessage("If notification types are enabled, at least one delivery method must be enabled")
//                 .OverridePropertyName("NotificationConfiguration");
//         }
//     }
//
//     public class UpdateUserNotificationCommandHandler : IRequestHandler<UpdateUserNotificationCommand, GenericResponse<string>>
//     {
//         public Task<GenericResponse<string>> Handle(UpdateUserNotificationCommand request, CancellationToken cancellationToken)
//         {
//             throw new NotImplementedException();
//         }
//     }
// }