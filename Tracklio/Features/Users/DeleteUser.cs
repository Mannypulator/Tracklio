using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Users;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Users;

public class DeleteUser : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapDelete("api/v1/users/{userId}", async (
                string userId,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var query = new DeleteUserCommand(userId);
                var response = await mediator.Send(query, ct);
                return response.ReturnedResponse();
            })
            .WithName("DeleteUser")
            .WithTags("Users")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Delete User Profile",
                Description =
                    "Delete User Profile",
                OperationId = "DeleteUser",
            })
            .Produces<GenericResponse<UserResponse>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public class DeleteUserCommand(string userId) : IRequest<GenericResponse<string>>
    {
        public string UserId { get; set; } = userId;
    }

    public class DeleteUserCommandHandler(RepositoryContext context) : IRequestHandler<DeleteUserCommand, GenericResponse<string>>
    {
        public async  Task<GenericResponse<string>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return GenericResponse<string>.Error(400, "Invalid user id");
            }
            
            var user = await context
                .Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken: cancellationToken);

            if (user is null)
            {
                return GenericResponse<string>.Error(404, "User not found");
            }
            
            context.Users.Remove(user);
            
            await context.SaveChangesAsync(cancellationToken);
            
            return GenericResponse<string>.Success("User deleted", null!);
        }
    }
}