using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Users;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Mappings;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Users;

public class GetUser : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/users/profile/{userId}", async (
                string userId,
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var query = new GetUserQuery(userId);
                var response = await mediator.Send(query, ct);
                return response.ReturnedResponse();
            })
            .WithName("UserProfile")
            .WithTags("Users")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get User Profile",
                Description =
                    "User Profile is gotten by the userId provided in the path",
                OperationId = "UserProfile",
            })
            .Produces<GenericResponse<UserResponse>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public record GetUserQuery(string UserId) : IRequest<GenericResponse<UserResponse>>;

    public class GetUserQueryValidator : AbstractValidator<GetUserQuery>
    {
        public GetUserQueryValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().Must(IsUserIdValid).WithMessage("Invalid user id");
        }

        private static bool IsUserIdValid(string userId)
        {
            return Guid.TryParse(userId, out _);
        }
    }

    public class GetUserHandler(RepositoryContext context) : IRequestHandler<GetUserQuery, GenericResponse<UserResponse>>
    {
        public async Task<GenericResponse<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(request.UserId);
            
            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken: cancellationToken);
            
            if (user == null)
            {
                return GenericResponse<UserResponse>.Error(4004, "User not found");
            }
            
            var vehicleCount = await context.Vehicles.AsNoTracking().CountAsync(x => x.UserId == userId && x.IsActive, cancellationToken: cancellationToken);
            
            var activeTicketCount = await context.ParkingTickets.AsNoTracking().CountAsync(t => 
                t.Vehicle.UserId == userId && t.Status == TicketStatus.Active, cancellationToken: cancellationToken);
            
            var userResponse = user.MapToUserResponse(vehicleCount,  activeTicketCount);
            
            return GenericResponse<UserResponse>.Success("Success", userResponse);
        }
    }
    
   
    
    
}