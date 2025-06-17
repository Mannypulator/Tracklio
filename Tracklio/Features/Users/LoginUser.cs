using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tracklio.Shared.Configurations;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Users;

public class LoginUser : ISlice
{
    private static  JwtSettings _jwtSettings;

    public LoginUser(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/login", async (
                LoginCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("Login")
            .WithTags("Users")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Logs in a user and returns a JWT token",
                Description = "Authenticates a user with email and password, returning a JWT token on success or an error message on failure.",
                OperationId = "Login"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public sealed record LoginCommand(string Email, string Password)
            : IRequest<GenericResponse<string>>;

    public sealed class LoginCommandHandler(
        RepositoryContext context
        )
        : IRequestHandler<LoginCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = context
                      .Users
                      .FirstOrDefault(x => x.Email.Trim() == request.Email.Trim());

            if (user is null)
            {
                return GenericResponse<string>.Error(404, "User does not exist");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return GenericResponse<string>.Error(400, "invalid credentials");
            }

            if (!user.EmailConfirmed)
            {
                return GenericResponse<string>.Error(400, "User email is not confirmed yet");
            }

            var jwtToken = GenerateJwtToken(user);

            return GenericResponse<string>.Success("user logged in successfully", jwtToken);
        }
    }

    private static string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("UserRole", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName,  user.LastName),
        };

        var jwt = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
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