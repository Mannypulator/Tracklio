using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Dto.Subscription;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Users;

public class GetPaymentHistory : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("/api/v1/user/{UserId}/payments", async ([FromBody] GetUserPaymentHistory query, [FromServices] IMediator mediator) =>
            {
                var result = await mediator.Send(query);
                return result.ReturnedResponse();
            })
            .WithName("GetUserPaymentHistory")
            .WithTags("Users")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get User Payment History",
                Description = "Retrieve the payment history for a specific user.",
                OperationId = "GetUserPaymentHistory",
            })
            .Produces<GenericResponse<IReadOnlyList<PaymentHistoryDto>>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }


    public record GetUserPaymentHistory(Guid UserId)
    : IRequest<GenericResponse<IReadOnlyList<PaymentHistoryDto>>>;

    public class GetUserPaymentHistoryHandler(RepositoryContext context) : IRequestHandler<GetUserPaymentHistory, GenericResponse<IReadOnlyList<PaymentHistoryDto>>>
    {
        public async Task<GenericResponse<IReadOnlyList<PaymentHistoryDto>>> Handle(GetUserPaymentHistory request, CancellationToken cancellationToken)
        {
            var payments = await context.PaymentTransactions
                    .AsNoTracking()
                    .Where(p => p.UserId == request.UserId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new PaymentHistoryDto
                    {
                        PaymentId = p.Id.ToString(),
                        Plan = p.PlanName,
                        Amount = p.Amount,
                        Status = p.Status,
                        PaymentDate = p.PaymentDate,
                        RenewalDate = p.RenewalDate
                    })
                    .ToListAsync();

            return GenericResponse<IReadOnlyList<PaymentHistoryDto>>.Success("Success", payments);
        }
    }
}
