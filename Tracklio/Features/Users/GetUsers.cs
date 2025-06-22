using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Users;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Mappings;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Users;

public class GetUsers : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/users/profile", async (
                IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var query = new GetUsersQuery();
                var response = await mediator.Send(query, ct);
                return response.ReturnedResponse();
            })
            .WithName("GetUsers")
            .WithTags("Users")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get User Profiles",
                Description =
                    "Get all User profiles",
                OperationId = "GetUsers",
            })
            .Produces<GenericResponse<UserResponse>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }
    
    
    public record GetUsersQuery(): IRequest<GenericResponse<IReadOnlyList<UserResponse>>>;

    public class GetUsersQueryHandler(RepositoryContext context) : IRequestHandler<GetUsersQuery, GenericResponse<IReadOnlyList<UserResponse>>>
    {
        public async Task<GenericResponse<IReadOnlyList<UserResponse>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
           var users = await context.Users.AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
           
           var userResponses = new List<UserResponse>();

           foreach (var user in users)
           {
               var vehicleCount = await context.Vehicles.AsNoTracking().CountAsync(x => x.UserId == user.Id && x.IsActive, cancellationToken: cancellationToken);
            
               var activeTicketCount = await context.ParkingTickets.AsNoTracking().CountAsync(t => 
                   t.Vehicle.UserId == user.Id && t.Status == TicketStatus.Active, cancellationToken: cancellationToken);
               
               userResponses.Add(user.MapToUserResponse(vehicleCount, activeTicketCount));
           }
           
           return GenericResponse<IReadOnlyList<UserResponse>>.Success("Successfully retrieved users", userResponses);
        }
    }
    
}
