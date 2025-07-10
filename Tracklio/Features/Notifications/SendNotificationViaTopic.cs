using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Notification;
using Tracklio.Shared.Services.Notification;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Notifications;

public class SendNotificationViaTopic : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/notifications/send-via-topic", async (
                SendTopicNotificationCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("SendNotificationViaTopic")
            .WithTags("Notifications")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Send Push Notification to  User device via subscribed topic",
                Description =
                    "Send Push Notification to  User device via Firebase using a topic the user's device was subscribed to",
                OperationId = "SendNotificationViaTopic"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record SendTopicNotificationCommand(string Topic, string Title, string Body,  Dictionary<string, string>?  Data): IRequest<GenericResponse<NotificationResponse>>;
    
    public class SendTopicNotificationHandler(IFirebaseService firebaseService): IRequestHandler<SendTopicNotificationCommand, GenericResponse<NotificationResponse>>
    {
        public async Task<GenericResponse<NotificationResponse>> Handle(SendTopicNotificationCommand request, CancellationToken cancellationToken)
        {
            var response = await firebaseService.SendToTopicAsync(request.Topic, request.Title,request.Body, request.Data!);
            
            return !response.Success 
                ? GenericResponse<NotificationResponse>.Error(500, response.Error) 
                : GenericResponse<NotificationResponse>.Success("Successfully sent notification", response);
        }
    }

    public class SendTopicNotificationCommandValidator : AbstractValidator<SendTopicNotificationCommand>
    {
        public SendTopicNotificationCommandValidator()
        {
            RuleFor(x => x.Topic).NotNull().NotEmpty();
            RuleFor(x => x.Title).NotNull().NotEmpty();
            RuleFor(x => x.Body).NotNull().NotEmpty();
        }
    }
}