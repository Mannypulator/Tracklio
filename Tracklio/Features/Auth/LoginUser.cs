using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tracklio.Shared.Configurations;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Auth;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services.Token;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public class LoginUser : ISlice
{

    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/login", async (
                LoginCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("Login")
            .WithTags("Auth")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Logs in a user and returns a JWT token",
                Description =
                    "Authenticates a user with email and password, returning a JWT token on success or an error message on failure.",
                OperationId = "Login"
            })
            .Produces<GenericResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<LoginResponse>>(StatusCodes.Status400BadRequest);
    }

    public sealed record LoginCommand(string Email, string Password, bool RememberMe)
        : IRequest<GenericResponse<LoginResponse>>;

    public sealed class LoginCommandHandler(
        RepositoryContext context,
        ITokenService tokenService
    )
        : IRequestHandler<LoginUser.LoginCommand, GenericResponse<LoginResponse>>
    {
        public async Task<GenericResponse<LoginResponse?>> Handle(LoginCommand request,
            CancellationToken cancellationToken)
        {
            var user = context
                .Users
                .AsNoTracking()
                .FirstOrDefault(x => x.Email.Trim() == request.Email.Trim());

            if (user is null)
            {
                return GenericResponse<LoginResponse>.Error(404, "User does not exist");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash) || !user.IsActive)
            {
                return GenericResponse<LoginResponse>.Error(400, "invalid credentials");
            }

            if (!user.EmailConfirmed)
            {
                return GenericResponse<LoginResponse>.Error(400, "User email is not confirmed yet");
            }

            var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
            var refreshToken = tokenService.GenerateRefreshToken();

            var userRefreshToken = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(request.RememberMe ? 30 : 7),
                CreatedAt = DateTime.UtcNow
            };

            user.LastLoginAt = DateTime.UtcNow;
            context.Users.Update(user);
            await context.UserRefreshTokens.AddAsync(userRefreshToken, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

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

            return GenericResponse<LoginResponse>.Success("user logged in successfully", loginResponse);
        }
    }
    

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Provide Otp Address")
                .EmailAddress().WithMessage("Enter a valid email address");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Provide password")
                .MinimumLength(6).WithMessage("Password length must be greater than 6");
        }
    }



}