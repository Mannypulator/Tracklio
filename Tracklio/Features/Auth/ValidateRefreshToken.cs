using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Auth;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services.Token;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public sealed class ValidateRefreshToken : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/refresh", async (
                ValidateRefreshTokenCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("RefreshToken")
            .WithTags("Auth")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Validate your refresh token",
                Description =
                    "Validate user refresh token to generate a new one",
                OperationId = "RefreshToken"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }

    public class ValidateRefreshTokenCommand: IRequest<GenericResponse<LoginResponse>>
    {
        public string RefreshToken { get; set; }
    }

    public class ValidateRefreshTokenCommandValidator : AbstractValidator<ValidateRefreshTokenCommand>
    {
        public ValidateRefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotNull().NotEmpty();
        }
    }

    public class ValidateRefreshTokenCommandHandler(ITokenService tokenService, RepositoryContext context): IRequestHandler<ValidateRefreshTokenCommand,
        GenericResponse<LoginResponse>>
    {
        public async Task<GenericResponse<LoginResponse>> Handle(ValidateRefreshTokenCommand request, CancellationToken cancellationToken)
        {
           var refreshToken = await 
                context
                    .UserRefreshTokens
                    .AsNoTracking()
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken: cancellationToken);
           
           if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
           {
              return GenericResponse<LoginResponse>.Error(400, "Invalid or expired refresh token");
           }
           
           var user = refreshToken.User;
           if (!user.IsActive)
           {
               return GenericResponse<LoginResponse>.Error(401, "User is not active");
           }

           
           var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
           var newRefreshToken = tokenService.GenerateRefreshToken();

        
           refreshToken.IsRevoked = true;
           refreshToken.RevokedAt = DateTime.UtcNow;
           refreshToken.RevokedByIp = null;
          

        
           var userRefreshToken = new UserRefreshToken
           {
               Id = Guid.NewGuid(),
               UserId = user.Id,
               Token = newRefreshToken,
               ExpiresAt = DateTime.UtcNow.AddDays(7),
               CreatedAt = DateTime.UtcNow
           };

           await context.UserRefreshTokens.AddAsync(userRefreshToken, cancellationToken);
           await context.SaveChangesAsync(cancellationToken);
           
           var loginResponse = new LoginResponse
           (
               AccessToken: accessToken,
               RefreshToken: newRefreshToken,
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
           
           return GenericResponse<LoginResponse>.Success("Token refreshed successfully", loginResponse);
           
        }
    }
    
    
}