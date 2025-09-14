// using System;
// using System.Security.Claims;
// using MediatR;
// using Tracklio.Shared.Slices;

// namespace Tracklio.Features.Payments;

// public class CreateCustomer : ISlice
// {
//     public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
//     {
//         endpointRouteBuilder.MapPost("api/v1/payments/create-customer", async (
//                 [FromBody] CreateCustomerCommand request,
//                 ClaimsPrincipal claims,
//                 [FromServices] IMediator mediator,
//                 CancellationToken ct
//             ) =>
//             {
//                 var userId = claims.GetUserIdAsGuid();
//                 // You might want to pass the userId in the request if needed
//                 request.UserId = userId;
//                 var response = await mediator.Send(request, ct);
//                 return response.ReturnedResponse();
//             })
//             .WithName("CreateCustomer")
//             .WithTags("Payments")
//             .WithOpenApi(operation => new OpenApiOperation(operation)
//             {
//                 Summary = "Create a new customer",
//                 Description = "Creates a new customer in Stripe.",
//                 OperationId = "CreateCustomer"
//             })
//             .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
//             .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest);
//     }

//     public record CreateCustomerCommand(CreateCustomerRequest Customer) : IRequest<GenericResponse<string>>
//     {
//         public Guid UserId { get; set; }
//     }
// }
