using Microsoft.EntityFrameworkCore;
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
        
        var emailBody = $"Kindly use this Otp:{code} to validate your account";
        
        await context.UserOtps.AddAsync(otp, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        await emailService.SendEmailAsync(request.Email, request.Reason, emailBody);
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