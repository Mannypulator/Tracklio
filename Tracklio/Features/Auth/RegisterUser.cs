using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services;
using Tracklio.Shared.Services.Token;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public sealed class RegisterUser : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/register", async (
                RegisterUserCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("RegisterUser")
            .WithTags("Auth")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Registers a new user",
                Description = "Creates a new user with the provided email and password. Sends a simulated email confirmation and returns a success or error message.",
                OperationId = "RegisterUser"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public record RegisterUserCommand
    (
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string PhoneNumber,
        string ConfirmPassword
    ): IRequest<GenericResponse<string>>;
    
    public sealed class RegisterUserHandler(
        RepositoryContext context,
        ITokenService tokenService,
        IEmailService emailService
        ): IRequestHandler<RegisterUser.RegisterUserCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await context
                .Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.Trim() == request.Email.Trim(), cancellationToken: cancellationToken);
            
            if (existingUser is not null)
            {
                return GenericResponse<string>.Error(409, "Email address is already registered.", existingUser.EmailConfirmed.ToString());
            }

            var userToBeSaved = request.MapToEntity();

            var notificationPreference = request.MapToNotificationPreferences(userToBeSaved.Id);
            
            var accessToken = tokenService.GenerateAccessToken(userToBeSaved.Id, userToBeSaved.Email, userToBeSaved.Role.ToString());
            var refreshToken = tokenService.GenerateRefreshToken();
            
            var code = Util.GenerateSecureOtp(6);
            
            var otp = new UserOtp()
            {
                OneTimePassword = code,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };
            
            var userRefreshToken = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userToBeSaved.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            
            await context.UserRefreshTokens.AddAsync(userRefreshToken, cancellationToken);
            await context.UserOtps.AddAsync(otp, cancellationToken);
            
            await context.Users.AddAsync(userToBeSaved, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            
            
            var emailBody = $"Kindly use this Otp:{code} to validate your account";
            
            

            await emailService.SendEmailAsync(request.Email, "VERIFY EMAIL", emailBody);
            
            return GenericResponse<string>.Success( "Successfully created user", userToBeSaved.Id.ToString());
        }
    }

    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email must be a valid email address")
                .MaximumLength(50)
                .WithMessage("Email must not exceed 50 characters");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
                .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit and one special character");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password confirmation is required")
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required")
                .MaximumLength(50)
                .WithMessage("First name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$")
                .WithMessage("First name can only contain letters, spaces, hyphens and apostrophes");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required")
                .MaximumLength(50)
                .WithMessage("Last name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$")
                .WithMessage("Last name can only contain letters, spaces, hyphens and apostrophes");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                .WithMessage("Phone number must be a valid international format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }

   
}

public static partial class UserMapping
{

    public static User MapToEntity(this RegisterUser.RegisterUserCommand dto)
    {
        return new User()
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.ToLowerInvariant(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Role = UserRole.Motorist,
            IsActive = true,
            EmailConfirmed = false,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };
    }


    public static NotificationPreferences MapToNotificationPreferences(this RegisterUser.RegisterUserCommand dto, Guid userId)
    {
        return new NotificationPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmailNotifications = true,
            SmsNotifications = !string.IsNullOrEmpty(dto.PhoneNumber),
            PushNotifications = true,
            NewTicketNotifications = true,
            PaymentReminderNotifications = true,
            AppealStatusNotifications = true,
            DeadlineReminderNotifications = true,
            ReminderDaysBefore = 3
        };
    }
}