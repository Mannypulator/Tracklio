using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Users;

public sealed class RegisterUser : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/users", async (
                RegisterUserCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("RegisterUser")
            .WithTags("Users")
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
        IEmailService emailService
        ): IRequestHandler<RegisterUserCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await context
                .Users
                .FirstOrDefaultAsync(x => x.Email.Trim() == request.Email.Trim(), cancellationToken: cancellationToken);
            
            if (existingUser is not null)
            {
                return GenericResponse<string>.Error(409, "Otp address is already registered.");
            }

            var userToBeSaved = request.MapToEntity();
            
            var code = Util.GenerateOtp();
            
            var otp = new UserOtp()
            {
                OneTimePassword = code,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };
            
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
                .WithMessage("Otp is required")
                .EmailAddress()
                .WithMessage("Otp must be a valid email address")
                .MaximumLength(50)
                .WithMessage("Otp must not exceed 256 characters");

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
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };
    }
}