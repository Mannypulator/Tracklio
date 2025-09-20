using System;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Subscription;

public class CreateSubscriptionPlan : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("/api/v1/subscription/plans", async (
            [FromBody] CreateSubscriptionPlanCommand command,
            [FromServices] IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return result.ReturnedResponse();
            })
            .WithName("CreateSubscriptionPlan")
            .WithTags("Subscriptions")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Create a new subscription plan",
                Description = "Allows admin to create a new subscription plan.",
                OperationId = "CreateSubscriptionPlan",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status409Conflict)
            .RequireAuthorization(PoliciesConstant.AdminOnly);
    }

    // Define commands, handlers, and any other necessary classes here
    public record CreateSubscriptionPlanCommand
    (
        string Name,
        string DisplayName,
        string Icon,
        decimal PriceMonthly,
        decimal PriceYearly,
        string Currency,
        string Description,
        int MaxVehicles
    ) : IRequest<GenericResponse<string>>;

    public class CreateSubscriptionPlanCommandHandler(RepositoryContext context) : IRequestHandler<CreateSubscriptionPlanCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            // Check if a plan with the same name already exists
            var existingPlan = await context.SubscriptionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.Trim().ToLower() == request.Name.Trim().ToLower(), cancellationToken);

            if (existingPlan != null)
            {
                return GenericResponse<string>.Error(409,"A subscription plan with the same name already exists.");
            }

            var newPlan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.DisplayName,
                Icon = request.Icon,
                PriceMonthly = request.PriceMonthly,
                PriceYearly = request.PriceYearly,
                Currency = request.Currency,
                Description = request.Description,
            };

            context.SubscriptionPlans.Add(newPlan);
            await context.SaveChangesAsync(cancellationToken);

            return GenericResponse<string>.Success("Subscription plan created successfully", newPlan.Id.ToString());
        }
    }


    public class CreateSubscriptionPlanValidator : AbstractValidator<CreateSubscriptionPlanCommand>
    {
        public CreateSubscriptionPlanValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.PriceMonthly).GreaterThan(0).WithMessage("Monthly price must be positive.");
            RuleFor(x => x.PriceYearly).GreaterThan(0).WithMessage("Yearly price must be positive.");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.Icon).NotEmpty().WithMessage("Icon is required.");
            RuleFor(x => x.MaxVehicles).GreaterThan(0).WithMessage("Max vehicles must be positive.");
        }
    }
}
