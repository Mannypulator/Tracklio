using Tracklio.Shared.Domain.Dto.Otp;

namespace Tracklio.Shared.Services.Otp;

public interface IOtpService
{
    Task<bool> SendOtpAsync(SendOtpRequest request,CancellationToken cancellationToken);
    Task<bool> ValidateOtpAsync(string email, string otp, CancellationToken cancellationToken);
}