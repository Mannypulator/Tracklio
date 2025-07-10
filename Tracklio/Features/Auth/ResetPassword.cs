using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services.Otp;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public class ResetPassword : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/reset-password", async (
                ResetPasswordCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("ResetPassword")
            .WithTags("Auth")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Reset Password",
                Description =
                    "Reset User Password Endpoint",
                OperationId = "ResetPassword"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record ResetPasswordCommand(string Otp, string Email, string Password): IRequest<GenericResponse<string>>;

    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Otp).NotEmpty().NotNull().Length(7);
            RuleFor(x => x.Email).EmailAddress().NotEmpty().NotNull();
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
                .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit and one special character");
        }
    }
    
    public class ResetPasswordHandler(RepositoryContext context, IOtpService otpService) : IRequestHandler<ResetPasswordCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string?>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
           var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email.Trim() == request.Email.Trim(), cancellationToken: cancellationToken);

           if (user == null)
           {
               return GenericResponse<string>.Error(404, "User not found");
           }
           
           var isOtpValid = await otpService.ValidateOtpAsync(request.Email, request.Otp, cancellationToken);

           if (!isOtpValid)
           {
               return GenericResponse<string>.Error(403, "Invalid OTP");
           }
           user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
           user.UpdatedAt = DateTime.UtcNow;
           await  context.SaveChangesAsync(cancellationToken);
           
           await context.UserRefreshTokens
               .Where(t => t.UserId == user.Id && !t.IsRevoked)
               .ExecuteUpdateAsync(setters => setters
                   .SetProperty(t => t.IsRevoked, true)
                   .SetProperty(t => t.RevokedAt, DateTime.UtcNow), cancellationToken: cancellationToken);
           
         
           
           return GenericResponse<string>.Success("Password reset was done successfully", null!);
           
        }
    }
}