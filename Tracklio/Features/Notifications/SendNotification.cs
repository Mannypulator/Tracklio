using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FirebaseAdmin.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Notification;
using Tracklio.Shared.Mappings;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services.Notification;
using Tracklio.Shared.Slices;
using NotificationPriority = Tracklio.Shared.Domain.Enums.NotificationPriority;

namespace Tracklio.Features.Notifications;

public class SendNotification : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/notifications/send", async (
                SendNotificationCommand request,
                ClaimsPrincipal claims,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                request.UserId = userId;
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("SendNotification")
            .WithTags("Notifications")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Send Push Notification to  User device",
                Description =
                    "Send Push Notification to  User device via Firebase",
                OperationId = "SendNotification"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public class SendNotificationCommand(Guid userId, string title, string body) : IRequest<GenericResponse<NotificationResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; set; } = userId;
        public string Title { get; set; } = title;
        public string Body { get; set; } = body;
    }
   
  
    
    public class SendNotificationCommandHandler(
        IFirebaseService firebaseService,
        RepositoryContext context
        ) : IRequestHandler<SendNotificationCommand, GenericResponse<NotificationResponse>>
    {
        

        public async Task<GenericResponse<NotificationResponse>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return GenericResponse<NotificationResponse>.Error(401, "User is not authorized");
            }
            var userDevice = await context.UserDevices.FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken: cancellationToken);
            var response = await firebaseService.SendNotificationAsync(request.MapToDto(userDevice.DeviceToken));
            return !response.Success 
                ? GenericResponse<NotificationResponse>.Error(500, response.Error) 
                : GenericResponse<NotificationResponse>.Success("Successfully sent notification", response);
        }
    }


    public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
    {
        public SendNotificationCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().NotNull();
            RuleFor(x => x.Title).NotEmpty().NotNull();
            RuleFor(x => x.Body).NotEmpty().NotNull();
        }
    }
}
