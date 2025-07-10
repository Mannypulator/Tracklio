using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Otp;

public class ValidateOtp : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/otp/verify-otp", async (
                ValidateOtpCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("VerifyOtp")
            .WithTags("Otp")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Verify Otp send to user",
                Description = "Verify Otp send to user",
                OperationId = "VerifyOtp"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound);
    }

    public class ValidateOtpCommand : IRequest<GenericResponse<string>>
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class ValidateOtpCommandHandler(RepositoryContext context) : IRequestHandler<ValidateOtpCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string?>> Handle(ValidateOtpCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await context
                .Users
                .FirstOrDefaultAsync(x => x.Email.Trim() == request.Email.Trim(), cancellationToken: cancellationToken);
            
            if (existingUser is null)
            {
                return GenericResponse<string>.Error(404, "User not found.");
            }
            
            var otpExists = await context
                .UserOtps
                .AnyAsync(u 
                    => u.Email.Trim() == request.Email.Trim() 
                       && u.OneTimePassword.Trim() == request.Otp.Trim(), cancellationToken: cancellationToken);

            if (!otpExists)
            {
                return GenericResponse<string>.Error(400, "Invalid OTP");
            }
            
            existingUser.EmailConfirmed = true;
            existingUser.UpdatedAt =  DateTime.UtcNow;
            
            await context.SaveChangesAsync(cancellationToken);
            
            return GenericResponse<string>.Success("Otp verified", null!);
            
        }
    }
}