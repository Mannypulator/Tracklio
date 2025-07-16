using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Notification;
using Tracklio.Shared.Services.Notification;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Notifications;

public class SubscribeToTopic : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/notifications/subscribe-to-topic", async (
                SubscribeToTopicCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("SubscribeToTopic")
            .WithTags("Notifications")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Subscribe user's device  to a topic",
                Description =
                    "Subscribe user's device  to a topic",
                OperationId = "SubscribeToTopic"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status401Unauthorized)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public record SubscribeToTopicCommand(TopicSubscriptionRequest Notification)
        : IRequest<GenericResponse<NotificationResponse>>;
    
    
    public class SubscribeToTopicHandler(IFirebaseService firebaseService) : IRequestHandler<SubscribeToTopicCommand, GenericResponse<NotificationResponse>>
    {
        public async Task<GenericResponse<NotificationResponse>> Handle(SubscribeToTopicCommand request, CancellationToken cancellationToken)
        {
            var response = await firebaseService.SubscribeToTopicAsync(request.Notification);
            return !response.Success 
                ? GenericResponse<NotificationResponse>.Error(500, response.Error) 
                : GenericResponse<NotificationResponse>.Success("Successfully sent notification", response);
        }
    }
    
    
    public class TopicSubscriptionRequestValidator : AbstractValidator<TopicSubscriptionRequest>
    {
        public TopicSubscriptionRequestValidator()
        {
            RuleFor(x => x.DeviceTokens)
                .NotNull()
                .WithMessage("Device tokens are required")
                .NotEmpty()
                .WithMessage("At least one device token is required")
                .Must(tokens => tokens.Count <= 1000)
                .WithMessage("Cannot subscribe more than 1000 tokens at once")
                .Must(tokens => tokens.All(token => !string.IsNullOrWhiteSpace(token)))
                .WithMessage("All device tokens must be valid")
                .Must(tokens => tokens.Distinct().Count() == tokens.Count)
                .WithMessage("Device tokens must be unique");

            RuleForEach(x => x.DeviceTokens)
                .Must(BeValidFirebaseToken)
                .WithMessage("Invalid device token format");

            RuleFor(x => x.Topic)
                .NotEmpty()
                .WithMessage("Topic is required")
                .Must(BeValidTopicName)
                .WithMessage("Topic name must match pattern [a-zA-Z0-9-_.~%]+ and be less than 900 characters");
        }

        private static bool BeValidFirebaseToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return token.Length >= 140 && 
                   Regex.IsMatch(token, @"^[A-Za-z0-9_:-]+$");
        }

        private static bool BeValidTopicName(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return false;

            return Regex.IsMatch(topic, @"^[a-zA-Z0-9\-_.~%]+$") && 
                   topic.Length <= 900;
        }
    }
}