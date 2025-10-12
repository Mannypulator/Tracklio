using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tracklio.EmailTemplates.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Otp;

public sealed class SendOtp : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/otp/send-otp", async (
                SendOtpCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("SendOtp")
            .WithTags("Otp")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "request to send otp to email",
                Description = "Send otp to the email requested for",
                OperationId = "SendOtp",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound);
    }

    public class SendOtpCommand : IRequest<GenericResponse<string>>
    {
        public string Email { get; set; }
    }
    
    public class SendOtpCommandHandler(RepositoryContext context, IEmailService emailService) : IRequestHandler<SendOtpCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string?>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await context
                .Users
                .FirstOrDefaultAsync(x => x.Email.Trim() == request.Email.Trim(), cancellationToken: cancellationToken);
            
            if (existingUser is null)
            {
                return GenericResponse<string>.Error(404, "User not found.");
            }
            
            var code = Util.GenerateSecureOtp(6);
            
            var otp = new UserOtp()
            {
                OneTimePassword = code,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };
            
            await context.UserOtps.AddAsync(otp, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            var emailModel = new EmailVerificationModel
            {
                CustomerName = $"{existingUser.FirstName} {existingUser.LastName}",
                VerificationCode = code,
                ExpiryMinutes = 10
            };

            await emailService.SendTemplatedEmailAsync(request.Email, "Verify Your Email - Tracklio", "EmailVerification", emailModel);

            return GenericResponse<string>.Success("Otp has been sent successfully", null!);
        }
        
        
    }
}