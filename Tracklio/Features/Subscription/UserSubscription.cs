// using System;
// using FluentValidation;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using Tracklio.Shared.Domain.Dto;
// using Tracklio.Shared.Persistence;
// using Tracklio.Shared.Slices;

// namespace Tracklio.Features.Subscription;

// public class UserSubscription : ISlice
// {
//     public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
//     {
//         throw new NotImplementedException();
//     }

//     // Define properties and methods related to user subscriptions here
//     public record UserSubscriptionCommand
//     (
//         Guid UserId,
//         Guid PlanId,
//         bool IsMonthly
//     ) : IRequest<GenericResponse<string>>;

//     public class UserSubscriptionCommandHandler(RepositoryContext context) : IRequestHandler<UserSubscriptionCommand, GenericResponse<string>>
//     {
//         public async Task<GenericResponse<string>> Handle(UserSubscriptionCommand request, CancellationToken cancellationToken)
//         {
//             // Implement the logic to handle user subscriptions here

//             var subscription = await context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
//                 .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.PlanId == request.PlanId && s.IsMonthly == request.IsMonthly, cancellationToken);
//             // For example, create or update a subscription record in the database

//             // This is a placeholder implementation
//             return GenericResponse<string>.Success("Subscription updated successfully", "SubscriptionIdPlaceholder");
//         }
//     }
    
//     public class UserSubscriptionCommandValidator : AbstractValidator<UserSubscriptionCommand>
//     {
//         public UserSubscriptionCommandValidator()
//         {
//             RuleFor(x => x.UserId).NotEmpty();
//             RuleFor(x => x.PlanId).NotEmpty();
//             RuleFor(x => x.IsMonthly).NotNull();
//         }
//     }
// }
