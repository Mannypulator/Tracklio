using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Admin;

public class UpdateIUserStatus : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // add swagger documentation
        endpointRouteBuilder.MapPut("/api/user/status", async (
            [FromBody] UpdateUserStatusCommand command, [FromServices] IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.ReturnedResponse();
        })
        .WithName("UpdateUserStatus")
        .WithTags("Users")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Update User Status",
            Description = "Activate or deactivate a user account.",
            OperationId = "UpdateUserStatus",
        })
        .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
        .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
        .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
        .RequireAuthorization("AdminPolicy");
    }

    public record UpdateUserStatusCommand(Guid UserId, bool IsActive) : IRequest<GenericResponse<string>>;


    public class UpdateUserStatusCommandHandler(RepositoryContext context) : IRequestHandler<UpdateUserStatusCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            var user = await context.Users.FindAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return GenericResponse<string>.Error(404, "User not found");
            }

            user.IsActive = request.IsActive;
            await context.SaveChangesAsync(cancellationToken);

            return GenericResponse<string>.Success("User status updated successfully", null!);
        }
    }
}
