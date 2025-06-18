namespace Tracklio.Shared.Domain.Dto.Auth;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo UserInfo
);

public record UserInfo(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    string FirstName,
    string LastName,
    string Role,
    DateTime? LastLoginAt
    );