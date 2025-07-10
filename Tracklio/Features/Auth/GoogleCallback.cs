using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Auth;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services.Token;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public class GoogleCallback :ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/api/auth/google-callback", async (
            HttpContext context,
            RepositoryContext repositoryContext,
            ITokenService tokenService,
            CancellationToken ct
        ) =>
        {
            var auth = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!auth.Succeeded)
            {
                return Results.Unauthorized();
            }

            var claims = auth.Principal!;
            var email = claims.FindFirst(c => c.Type == ClaimTypes.Email)?.Value!;
            var firstName = claims.FindFirst(c => c.Type == ClaimTypes.GivenName)?.Value!;
            var lastName = claims.FindFirst(c => c.Type == ClaimTypes.Surname)?.Value!;

            var user = await repositoryContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken: ct);
            if (user == null)
            {
                user = new User()
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = "",
                    Role = UserRole.Motorist,
                    IsActive = true,
                    EmailConfirmed = true,
                    PasswordHash = "",
                    CreatedAt = DateTime.UtcNow
                };

                await repositoryContext.Users.AddAsync(user, cancellationToken: ct);
                await repositoryContext.SaveChangesAsync(ct);
            }

            var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
            var refreshToken = tokenService.GenerateRefreshToken();

            var userRefreshToken = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            user.LastLoginAt = DateTime.UtcNow;
            repositoryContext.Users.Update(user);
            await repositoryContext.UserRefreshTokens.AddAsync(userRefreshToken, ct);
            await repositoryContext.SaveChangesAsync(ct);

            var loginResponse = new LoginResponse
            (
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresAt: DateTime.UtcNow.AddMinutes(5),
                UserInfo: new UserInfo
                (
                    user.Id,
                    user.Email,
                    user.EmailConfirmed,
                    user.FirstName,
                    user.LastName,
                    user.Role.ToString(),
                    user.LastLoginAt
                )
            );

            var response = GenericResponse<LoginResponse>.Success("user logged in successfully", loginResponse);
            return response.ReturnedResponse();
        }).WithTags("Auth");
    }
}