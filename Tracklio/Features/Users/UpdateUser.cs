using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Users;

public class UpdateUser : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPut("api/v1/users/update", async (
                ClaimsPrincipal claims,
               [FromBody] UpdateUserCommand request,
               [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                request.UserId = userId;
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("UpdateUser")
            .WithTags("Users")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Update User Profile",
                Description =
                    "Update user profilr",
                OperationId = "UpdateUser",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }

    public class UpdateUserCommand : IRequest<GenericResponse<string>>
    {
        [JsonIgnore]
        public Guid UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email must be a valid email address")
                .MaximumLength(50)
                .WithMessage("Email must not exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required")
                .MaximumLength(50)
                .WithMessage("First name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$")
                .WithMessage("First name can only contain letters, spaces, hyphens and apostrophes")
                .When(x => !string.IsNullOrEmpty(x.FirstName));

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required")
                .MaximumLength(50)
                .WithMessage("Last name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$")
                .WithMessage("Last name can only contain letters, spaces, hyphens and apostrophes")
                .When(x => !string.IsNullOrEmpty(x.LastName));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[\+]?[1-9][\d]{0,15}$")
                .WithMessage("Phone number must be a valid international format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }

        public class UpdateUserCommandHandler(RepositoryContext context) : IRequestHandler<UpdateUserCommand, GenericResponse<string>>
        {
            public async Task<GenericResponse<string>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
            {
                if (request.UserId == Guid.Empty)
                {
                    return GenericResponse<string>.Error(400, "User ID is required");
                }
                
                var user  = await context
                    .Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken: cancellationToken);

                if (user is null)
                {
                    return GenericResponse<string>.Error(404, "User not found");
                }
                
                user.Email = string.IsNullOrEmpty(request.Email) ? user.Email : request.Email;
                user.FirstName = string.IsNullOrEmpty(request.FirstName) ? user.FirstName : request.FirstName;
                user.LastName = string.IsNullOrEmpty(request.LastName) ? user.LastName : request.LastName;
                user.PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? user.PhoneNumber : request.PhoneNumber;
                user.UpdatedAt = DateTime.UtcNow;
                context.Users.Update(user);
                await context.SaveChangesAsync(cancellationToken);
                
                return GenericResponse<string>.Success("User successfully updated", null!);
            }
        }
    }
}