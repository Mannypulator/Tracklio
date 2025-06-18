namespace Tracklio.Shared.Services.Token;
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId);
    Task<string> GeneratePasswordResetTokenAsync(Guid userId);
    Task<bool> ValidateEmailConfirmationTokenAsync(Guid userId, string token);
    Task<bool> ValidatePasswordResetTokenAsync(Guid userId, string token);
    Guid? GetUserIdFromToken(string token);
    bool IsTokenExpired(string token);
}