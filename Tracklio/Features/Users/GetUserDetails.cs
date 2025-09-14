using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Admin;
using Tracklio.Shared.Domain.Dto.Vehicle;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Admin;

public class GetUserDetails : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/v1/users/{userId}/details", async (
                [FromServices] IMediator mediator,
                [FromRoute] Guid userId,
                CancellationToken ct
            ) =>
            {
                var response = await mediator.Send(new GetUserDetailsQuery(userId), ct);
                return response.ReturnedResponse();
            })
            .WithName("GetUserDetails")
            .WithTags("Users")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get User Details for Admin",
                Description =
                    "Get detailed information about a specific user, including their vehicles and active parking tickets.",
                OperationId = "GetUserDetailsForAdmin",
            })
            .Produces<GenericResponse<UserDetailsDto>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
            .RequireAuthorization("AdminPolicy");
    }

    public record GetUserDetailsQuery(Guid UserId) : IRequest<GenericResponse<UserDetailsDto>>;

    public class GetUserDetailsQueryHandler(RepositoryContext context) : IRequestHandler<GetUserDetailsQuery, GenericResponse<UserDetailsDto>>
    {
        public async Task<GenericResponse<UserDetailsDto>> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
        {
            var user = await context.Users
                .AsNoTracking()
                .Include(u => u.Vehicles)
                .ThenInclude(v => v.ParkingTickets)
                .Where(u => u.Id == request.UserId)
                .Select(u => new UserDetailsDto
                {
                    User = new UserDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Role = nameof(u.Role),
                        HasSubscription = u.HasSubscription,
                        IsActive = u.IsActive,
                        VehicleCount = u.Vehicles.Count(v => v.IsActive),
                        ActivePcnCount = u.Vehicles.SelectMany(v => v.ParkingTickets).Count(pt => pt.Status == TicketStatus.Active),
                        ProfileImage = u.ProfileImage
                    },
                    Vehicles = u.Vehicles.Where(v => v.IsActive).Select(v => new VehicleDto
                    {
                        Id = v.Id,
                        VRM = v.VRM,
                        Make = v.Make,
                        Model = v.Model,
                        Color = v.Color,
                        Year = v.Year,
                        IsActive = v.IsActive
                    }).ToList(),
                    Tickets = u.Vehicles.SelectMany(v => v.ParkingTickets).Where(pt => pt.Status == TicketStatus.Active).Select(pt => new ParkingTicketDto
                    {
                        Id = pt.Id,
                        PCNReference = pt.PCNReference,
                        VRM = pt.VRM,
                        IssuedDate = pt.IssuedDate,
                        Location = pt.Location,
                        Reason = pt.Reason,
                        Amount = pt.Amount,
                        DiscountedAmount = pt.DiscountedAmount,
                        PaymentDeadline = pt.PaymentDeadline,
                        AppealDeadline = pt.AppealDeadline,
                        Status = pt.Status.ToString(),
                        IssuingAuthority = pt.IssuingAuthority,
                        PaymentUrl = pt.PaymentUrl,
                        AppealUrl = pt.AppealUrl
                    }).ToList()
                }).FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (user == null)
            {
                return GenericResponse<UserDetailsDto>.Error(404, "User not found.");
            }

            return GenericResponse<UserDetailsDto>.Success("Successfully retrieved user details.", user);
        }
    }

}
