using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Otp;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services;
using Tracklio.Shared.Services.Otp;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public class ForgotPassword : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/forget-password", async (
                ForgetPasswordCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("ForgetPassword")
            .WithTags("Auth")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Forget password",
                Description =
                    "Send Otp to user to reset your password.",
                OperationId = "ForgetPassword",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record ForgetPasswordCommand(string Email): IRequest<GenericResponse<string>>;

    public class ForgetPasswordCommandValidator : AbstractValidator<ForgetPasswordCommand>
    {
        public ForgetPasswordCommandValidator()
        {
            RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress().WithMessage("Invalid email address");
        }
    }

    public class ForgetPasswordCommandHandler(RepositoryContext context, IEmailService emailService, IOtpService otpService) 
        : IRequestHandler<ForgetPasswordCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string?>> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
        {
            var exitingUser  = context
                    .Users
                    .AsNoTracking().FirstOrDefault(x => x.Email.Trim() == request.Email.Trim());

            if (exitingUser is null)
            {
                return GenericResponse<string>.Error(404, "User not found");
            }
            
            var sendOtpRequest = new SendOtpRequest(exitingUser.Email, "Forget Password");
            
            await otpService.SendOtpAsync(sendOtpRequest, cancellationToken);
            
            return GenericResponse<string>.Success("Otp sent for forget password", null!);
        }
    }
    
    
}