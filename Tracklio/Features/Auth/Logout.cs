using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Auth;

public class Logout : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/auth/logout", async (
                LogOutCommand request,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("Logout")
            .WithTags("Auth")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Logs out a user",
                Description =
                    "Logouts a user by revalidating the refresh token",
                OperationId = "Logout"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }
    
    public record LogOutCommand(string RefreshToken): IRequest<GenericResponse<string>>;

    public class LogoutCommandValidator : AbstractValidator<LogOutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty().NotNull();
        }
    }

    public class LogOutCommandHandler(RepositoryContext context) : IRequestHandler<LogOutCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(LogOutCommand request, CancellationToken cancellationToken)
        {
            var refreshToken = await 
                context
                    .UserRefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken: cancellationToken);
            
            refreshToken!.IsRevoked = true;
            refreshToken.ExpiresAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            
            return GenericResponse<string>.Success("Logout was successful", null!);
            
        }
    }
}