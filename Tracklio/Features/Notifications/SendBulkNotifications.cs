// using System.Text.Json.Serialization;
// using System.Text.RegularExpressions;
// using FluentValidation;
// using MediatR;
// using Microsoft.OpenApi.Models;
// using Tracklio.Shared.Domain.Dto;
// using Tracklio.Shared.Domain.Dto.Notification;
// using Tracklio.Shared.Services.Notification;
// using Tracklio.Shared.Slices;
//
// namespace Tracklio.Features.Notifications;
//
// public sealed class SendBulkNotifications : ISlice
// {
//     public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
//     {
//         endpointRouteBuilder.MapPost("api/v1/notifications/send-bulk", async (
//                 SendBulkNotificationCommand request,
//                 IMediator mediator,
//                 CancellationToken ct
//             ) =>
//             {
//                 var response = await mediator.Send(request, ct);
//                 return response.ReturnedResponse();
//             })
//             .WithName("SendBulkNotification")
//             .WithTags("Notifications")
//             .WithOpenApi(operation => new OpenApiOperation(operation)
//             {
//                 Summary = "Send Bulk Push Notification to  User device",
//                 Description =
//                     "Send Bulk Push Notification to  User device via Firebase",
//                 OperationId = "SendBulkNotification"
//             })
//             .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
//             .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
//     }
//
//     public class SendBulkNotificationCommand : IRequest<GenericResponse<NotificationResponse>>
//     {
//         [JsonIgnore]
//         public Guid UserId { get; set; }
//         public string Title { get; set; }
//         public string Message { get; set; }
//         
//     }
//     
//     public class SendBulkNotificationHandler(IFirebaseService firebaseService) : IRequestHandler<SendBulkNotificationCommand, GenericResponse<NotificationResponse>>
//     {
//         public async Task<GenericResponse<NotificationResponse>> Handle(SendBulkNotificationCommand request, CancellationToken cancellationToken)
//         {
//            var response = await firebaseService.SendBulkNotificationAsync(request.Notification);
//            
//            return !response.Success 
//                ? GenericResponse<NotificationResponse>.Error(500, response.Error) 
//                : GenericResponse<NotificationResponse>.Success("Successfully sent notification", response);
//         }
//     }
//     
//      public class BulkNotificationRequestValidator : AbstractValidator<BulkNotificationRequest>
//     {
//         public BulkNotificationRequestValidator()
//         {
//             RuleFor(x => x.Title)
//                 .NotEmpty()
//                 .WithMessage("Title is required")
//                 .MaximumLength(100)
//                 .WithMessage("Title cannot exceed 100 characters");
//
//             RuleFor(x => x.Body)
//                 .NotEmpty()
//                 .WithMessage("Body is required")
//                 .MaximumLength(500)
//                 .WithMessage("Body cannot exceed 500 characters");
//
//             RuleFor(x => x.DeviceTokens)
//                 .NotNull()
//                 .WithMessage("Device tokens are required")
//                 .NotEmpty()
//                 .WithMessage("At least one device token is required")
//                 .Must(tokens => tokens.Count <= 500)
//                 .WithMessage("Cannot send to more than 500 device tokens at once")
//                 .Must(tokens => tokens.All(token => !string.IsNullOrWhiteSpace(token)))
//                 .WithMessage("All device tokens must be valid")
//                 .Must(tokens => tokens.Distinct().Count() == tokens.Count)
//                 .WithMessage("Device tokens must be unique");
//
//             RuleForEach(x => x.DeviceTokens)
//                 .Must(BeValidFirebaseToken)
//                 .WithMessage("Invalid device token format");
//
//             RuleFor(x => x.ImageUrl)
//                 .Must(BeValidImageUrl)
//                 .When(x => !string.IsNullOrEmpty(x.ImageUrl))
//                 .WithMessage("Image URL must be a valid HTTPS URL");
//             
//         }
//
//         private static bool BeValidFirebaseToken(string token)
//         {
//             if (string.IsNullOrWhiteSpace(token))
//                 return false;
//
//             return token.Length >= 140 && 
//                    Regex.IsMatch(token, @"^[A-Za-z0-9_:-]+$");
//         }
//
//         private static bool BeValidImageUrl(string imageUrl)
//         {
//             if (string.IsNullOrWhiteSpace(imageUrl))
//                 return false;
//
//             return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
//                    uri.Scheme == Uri.UriSchemeHttps;
//         }
//     }
//
// }