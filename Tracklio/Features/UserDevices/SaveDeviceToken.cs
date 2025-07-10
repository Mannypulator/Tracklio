using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Notification;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.UserDevices;

public sealed class SaveDeviceToken : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/devices/save-device-token", async (
                DeviceTokenRequest request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(new SaveDeviceTokenCommand(request), ct);
                return response.ReturnedResponse();
            })
            .WithName("SaveDeviceToken")
            .WithTags("Device")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Saved device token",
                Description =
                    "Saved device token to tracklio from firebase.",
                OperationId = "SaveDeviceToken"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status401Unauthorized)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public record SaveDeviceTokenCommand(DeviceTokenRequest Device) : IRequest<GenericResponse<string>>;
    
    public class SaveDeviceTokenCommandHandler(RepositoryContext context, ILogger<SaveDeviceTokenCommandHandler> logger) : IRequestHandler<SaveDeviceTokenCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(SaveDeviceTokenCommand request, CancellationToken cancellationToken)
        {
            var existingDevice = await context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == request.Device.DeviceToken, cancellationToken: cancellationToken);

            if (!Guid.TryParse(request.Device.UserId, out var userId))
            {
                return GenericResponse<string>.Error(400, "Invalid user id");
            }

            if (existingDevice != null)
            {
                // Update existing token
                existingDevice.UserId = userId;
                existingDevice.Platform = request.Device.Platform.ToLower();
                existingDevice.UpdatedAt = DateTime.UtcNow;
                existingDevice.IsActive = true;
            }
            else
            {
                // Create new token entry
                var userDevice = new UserDevice
                {
                    UserId = userId,
                    DeviceToken = request.Device.DeviceToken,
                    Platform = request.Device.Platform.ToLower(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                context.UserDevices.Add(userDevice);
            }
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation($"Device token saved for user {request.Device.UserId}");
            
            return GenericResponse<string>.Success("Device token saved", null!);
        }
    }


    public class DeviceTokenRequestValidator : AbstractValidator<DeviceTokenRequest>
    {
        private readonly string[] _validPlatforms = ["ios", "android", "web"];

        public DeviceTokenRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required")
                .MaximumLength(128)
                .WithMessage("User ID cannot exceed 128 characters")
                .Matches(@"^[a-zA-Z0-9\-_]+$")
                .WithMessage("User ID can only contain alphanumeric characters, hyphens, and underscores");

            RuleFor(x => x.DeviceToken)
                .NotEmpty()
                .WithMessage("Device token is required")
                .Must(BeValidFirebaseToken)
                .WithMessage("Invalid device token format");

            RuleFor(x => x.Platform)
                .Must(platform => string.IsNullOrEmpty(platform) || _validPlatforms.Contains(platform.ToLower()))
                .WithMessage($"Platform must be one of: {string.Join(", ", _validPlatforms)}");
        }

        private static bool BeValidFirebaseToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return token.Length >= 140 &&
                   Regex.IsMatch(token, @"^[A-Za-z0-9_:-]+$");
        }

    }
    
}