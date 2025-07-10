using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Configurations;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Auth;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services.Token;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public class VerifyGoogleToken : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/google-signin", async (
                GoogleTokenRequestCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("GoogleSignIn")
            .WithTags("Auth")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Logs in a user using Google SSO",
                Description =
                    "Authenticates a user with Google SSO, returning a JWT token on success or an error message on failure.",
                OperationId = "GoogleSignIn"
            })
            .Produces<GenericResponse<LoginResponse>>(StatusCodes.Status401Unauthorized)
            .Produces<GenericResponse<LoginResponse>>(StatusCodes.Status400BadRequest);
    }

    public record GoogleTokenRequestCommand(string GoogleToken) : IRequest<GenericResponse<LoginResponse?>>;
    
    public class GoogleUserInfo
    {
        public string Sub { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public bool Email_Verified { get; set; }
    }
    
    public class GoogleTokenRequestCommandHandler : IRequestHandler<GoogleTokenRequestCommand, GenericResponse<LoginResponse?>>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Authentication _authentication;
        private readonly ITokenService _tokenService;
        private readonly RepositoryContext _context;

        public GoogleTokenRequestCommandHandler(IOptions<Authentication> authentication, IHttpClientFactory httpClientFactory, ITokenService tokenService, RepositoryContext context)
        {
            _authentication = authentication.Value;
            _httpClientFactory = httpClientFactory;
            _tokenService = tokenService;
            _context = context;
        }

        public async Task<GenericResponse<LoginResponse?>> Handle(GoogleTokenRequestCommand request, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("GoogleAPI");
            var url = $"{_authentication.Google.AuthUrl}/tokeninfo?id_token={request.GoogleToken}";

            var response = await httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return GenericResponse<LoginResponse?>.Error(401, "Invalid Google token");
            }
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content)!;
            
            var user = await _context
                    .Users
                    .FirstOrDefaultAsync(x => x.Email.ToLower().Trim() == tokenInfo.Email.Trim().ToLower(), cancellationToken: cancellationToken);

            if (user is null)
            {
                user = new User()
                {
                    Id = Guid.NewGuid(),
                    Email = tokenInfo.Email,
                    FirstName = tokenInfo.Name,
                    LastName = tokenInfo.Name,
                    PhoneNumber = "",
                    Role = UserRole.Motorist,
                    IsActive = true,
                    EmailConfirmed = true,
                    PasswordHash = "",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user, cancellationToken: cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            
            
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
            var refreshToken = _tokenService.GenerateRefreshToken();

            var userRefreshToken = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            user.LastLoginAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.UserRefreshTokens.AddAsync(userRefreshToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

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
            
            
            
            return tokenInfo?.Sub == null 
                ? GenericResponse<LoginResponse?>.Error(401, "Invalid token format") 
                : GenericResponse<LoginResponse?>.Success("Google token verified successfully", loginResponse);
        }
    }
}