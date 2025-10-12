using Microsoft.EntityFrameworkCore;
using Tracklio.EmailTemplates.Models;
using Tracklio.Shared.Domain.Dto.Otp;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;

namespace Tracklio.Shared.Services.Otp;

public class OtpService(IEmailService emailService, RepositoryContext context) : IOtpService
{
    public async Task<bool> SendOtpAsync(SendOtpRequest request, CancellationToken cancellationToken)
    {
        var code = Util.GenerateOtp();

        var otp = new UserOtp()
        {
            OneTimePassword = code,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        var emailModel = new EmailVerificationModel
        {
            CustomerName = user != null ? $"{user.FirstName} {user.LastName}" : "User",
            VerificationCode = code,
            ExpiryMinutes = 10
        };

        await context.UserOtps.AddAsync(otp, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await emailService.SendTemplatedEmailAsync(request.Email, request.Reason, "EmailVerification", emailModel);
        return true;
    }

    public async Task<bool> ValidateOtpAsync(string email, string otp, CancellationToken cancellationToken)
    {
        var otpExists = await context
            .UserOtps
            .AsNoTracking()
            .AnyAsync(u 
                => u.Email.Trim() == email.Trim()
                   && u.OneTimePassword.Trim() == otp.Trim(), cancellationToken: cancellationToken);
        
        return otpExists;
    }
}