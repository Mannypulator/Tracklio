using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public sealed class EmailVerification : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/verify-email", async (
                HttpContext httpContext,
                VerifyEmailCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("VerifyEmail")
            .WithTags("Auth")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Verify onboarded user's email",
                Description = "Verify the email provided by a user",
                OperationId = "VerifyEmail"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public class VerifyEmailCommand : IRequest<GenericResponse<string>>
    {
      
        public string Email { get; set; }
        public string Otp { get; set; }
    };

    public class VerifyEmailCommandHandler(RepositoryContext context) : IRequestHandler<EmailVerification.VerifyEmailCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string?>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
                       
            var otpExists = await context
                .UserOtps
                .AsNoTracking()
                .AnyAsync(u 
                    => u.Email.Trim() == request.Email.Trim() 
                       && u.OneTimePassword.Trim() == request.Otp.Trim(), cancellationToken: cancellationToken);

            if (!otpExists)
            {
                return GenericResponse<string>.Error(400, "Invalid OTP");
            }
            
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Trim() == request.Email.Trim(), cancellationToken);

            if (user == null)
            {
                return GenericResponse<string>.Error(404, "User not found");
            }
            
            user.EmailConfirmed = true;
            user.UpdatedAt =  DateTime.UtcNow;
            
            await context.SaveChangesAsync(cancellationToken);
            
            return GenericResponse<string>.Success("Otp verified", null!);
            
        }
    }

    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.Otp).NotNull().NotEmpty().Length(6);
            RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress().WithMessage("Invalid email address");
        }
    }

}