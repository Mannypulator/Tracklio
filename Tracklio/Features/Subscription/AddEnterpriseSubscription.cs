using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Admin;

public class AddEnterpriseSubscription : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // add swagger documentation
        endpointRouteBuilder.MapPost("/api/v1/subscription/enterprise", async ([FromBody] AddEnterpriseCommand command, [FromServices] IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.ReturnedResponse();
        })
        .WithName("AddEnterpriseSubscription")
        .WithTags("Subscriptions")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Add Enterprise Subscription",
            Description = "Add a new enterprise subscription plan.",
            OperationId = "AddEnterpriseSubscription",
        })
        .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
        .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminPolicy");
    }

    public record AddEnterpriseCommand
    (
        string Name,
        string Email,
        string PlanName,
        int VehiclesAllowed,
        DateTime StartDate,
        DateTime EndDate,
        decimal PlanPrice,
        decimal DurationPrice
    ) : IRequest<GenericResponse<string>>;


    public class AddEnterpriseCommandHandler(RepositoryContext context) : IRequestHandler<AddEnterpriseCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(AddEnterpriseCommand request, CancellationToken cancellationToken)
        {
            var enterprise = new EnterprisePlan
            {
                Name = request.Name,
                Email = request.Email,
                PlanName = request.PlanName,
                VehiclesAllowed = request.VehiclesAllowed,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PlanPrice = request.PlanPrice,
                DurationPrice = request.DurationPrice,
                CreatedAt = DateTime.UtcNow
            };

            await context.EnterprisePlans.AddAsync(enterprise, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return GenericResponse<string>.Success("Success", null!);
        }
    }
}
