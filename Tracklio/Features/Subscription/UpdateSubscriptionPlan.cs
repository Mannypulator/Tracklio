using System;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Subscription;

public class UpdateSubscriptionPlan : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPut("/api/v1/subscription/plans", async (
            [FromBody] UpdateSubscriptionPlanCommand command,
            [FromServices] IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return result.ReturnedResponse();
            })
            .WithName("UpdateSubscriptionPlan")
            .WithTags("Subscriptions")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Update an existing subscription plan",
                Description = "Allows admin to update an existing subscription plan.",
                OperationId = "UpdateSubscriptionPlan",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .Produces<GenericResponse<string>>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PoliciesConstant.AdminOnly);
    }

    // Define commands, handlers, and any other necessary classes here
    public record UpdateSubscriptionPlanCommand
    (
        Guid PlanId,
        string Name,
        string DisplayName,
        string Icon,
        decimal PriceMonthly,
        decimal PriceYearly,
        string Currency,
        string Description,
        int MaxVehicles
    ) : IRequest<GenericResponse<string>>;

    public class UpdateSubscriptionPlanCommandHandler(RepositoryContext context) : IRequestHandler<UpdateSubscriptionPlanCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
        {
            var existingPlan = await context.SubscriptionPlans.FindAsync(new object[] { request.PlanId }, cancellationToken);

            if (existingPlan == null)
            {
                return GenericResponse<string>.Error(404, "Subscription plan not found.");
            }

            existingPlan.Name = request.Name;
            existingPlan.DisplayName = request.DisplayName;
            existingPlan.Icon = request.Icon;
            existingPlan.PriceMonthly = request.PriceMonthly;
            existingPlan.PriceYearly = request.PriceYearly;
            existingPlan.Currency = request.Currency;
            existingPlan.Description = request.Description;
            existingPlan.MaxVehicles = request.MaxVehicles;

            await context.SaveChangesAsync(cancellationToken);

            return GenericResponse<string>.Success("Subscription plan updated successfully", existingPlan.Id.ToString());
        }
    }

    public class UpdateSubscriptionPlanCommandValidator : AbstractValidator<UpdateSubscriptionPlanCommand>
    {
        public UpdateSubscriptionPlanCommandValidator()
        {
            RuleFor(x => x.PlanId).NotEmpty().WithMessage("Plan ID is required.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Display name is required.");
            RuleFor(x => x.PriceMonthly).GreaterThan(0).WithMessage("Monthly price must be positive.");
            RuleFor(x => x.PriceYearly).GreaterThan(0).WithMessage("Yearly price must be positive.");
            RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.Icon).NotEmpty().WithMessage("Icon is required.");
            RuleFor(x => x.MaxVehicles).GreaterThan(0).WithMessage("Max vehicles must be positive.");
        }
    }


    
}
