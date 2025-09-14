using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Stripe;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Payments;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Services.Stripe;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Payments;

public class CreateSubscription : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/payments/create-subscription", async (
                [FromBody] CreateSubscriptionCommand request,
                ClaimsPrincipal claims,
                [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                // You might want to pass the userId in the request if needed
                request.UserId = userId;
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("CreateSubscription")
            .WithTags("Payments")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Create a new subscription for a user",
                Description =
                    "Creates a new subscription using Stripe for the specified plan and billing period.",
                OperationId = "CreateSubscription"
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
    }

    public record CreateSubscriptionCommand(CreateSubscriptionRequest Subscription) : IRequest<GenericResponse<SubscriptionResponse>>
    {
        public Guid UserId { get; set; }
    }

    public class CreateSubscriptionCommandHandler(IStripeService stripeService) : IRequestHandler<CreateSubscriptionCommand, GenericResponse<SubscriptionResponse>>
    {
        public async Task<GenericResponse<SubscriptionResponse>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var subscriptionResponse = await stripeService.CreateSubscriptionAsync(request.UserId, request.Subscription);

            if (subscriptionResponse == null)
            {
                return GenericResponse<SubscriptionResponse>.Error(400, "Failed to create subscription.");
            }

            return GenericResponse<SubscriptionResponse>.Success("Subscription created successfully.", subscriptionResponse);

        }

    }
}
