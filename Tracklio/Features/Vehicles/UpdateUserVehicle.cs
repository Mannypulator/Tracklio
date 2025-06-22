using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Vehicles;

public class UpdateUserVehicle : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPut("api/v1/vehicles", async (
                ClaimsPrincipal claims,
                [FromBody] UpdateVehicleCommand request,
                [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                request.UserId = userId;
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("UpdateUserVehicle")
            .WithTags("Vehicles")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Endpoint to update a vehicle",
                Description =
                    "Allows user to update their vehicle.",
                OperationId = "UpdateUserVehicle",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PoliciesConstant.MotoristOrAdmin);
    }


    public class UpdateVehicleCommand : IRequest<GenericResponse<string>>
    {
        [JsonIgnore]
        public Guid UserId { get; set; }
        public string VehicleId { get; set; }
        public string? Model { get; set; }
        public string? Make { get; set; }
        public string? Color { get; set; }
        public int? Year { get; set; }
        
    }

    public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
    {
        public UpdateVehicleCommandValidator()
        {

            RuleFor(x => x.Make)
                .MaximumLength(50)
                .WithMessage("Make must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\s\-&]+$")
                .WithMessage("Make can only contain letters, numbers, spaces, hyphens and ampersands")
                .When(x => !string.IsNullOrEmpty(x.Make));

            RuleFor(x => x.Model)
                .MaximumLength(50)
                .WithMessage("Model must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\s\-&().]+$")
                .WithMessage("Model can only contain letters, numbers, spaces, hyphens, ampersands and brackets")
                .When(x => !string.IsNullOrEmpty(x.Model));

            RuleFor(x => x.Color)
                .MaximumLength(30)
                .WithMessage("Color must not exceed 30 characters")
                .Matches(@"^[a-zA-Z\s\-]+$")
                .WithMessage("Color can only contain letters, spaces and hyphens")
                .When(x => !string.IsNullOrEmpty(x.Color));

            RuleFor(x => x.Year)
                .GreaterThanOrEqualTo(1900)
                .WithMessage("Year must be 1900 or later")
                .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
                .WithMessage($"Year must not exceed {DateTime.UtcNow.Year + 1}")
                .When(x => x.Year.HasValue);
        }
    }

    public class UpdateVehicleCommandHandler(RepositoryContext context) : IRequestHandler<UpdateVehicleCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return GenericResponse<string>.Error(401, "Unauthorized");
            }

            if (!Guid.TryParse(request.VehicleId, out var vehicleId))
            {
                return GenericResponse<string>.Error(400, "Enter a valid vehicle id");
            }
            
            var vehicle = await context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);

            if (vehicle is null)
            {
                return GenericResponse<string>.Error(404, "Vehicle not found");
            }
            
            vehicle.Model =  string.IsNullOrEmpty(request.Model) ? vehicle.Model : request.Model;
            vehicle.Make =  string.IsNullOrEmpty(request.Make) ? vehicle.Make : request.Make;
            vehicle.Color = string.IsNullOrEmpty(request.Color) ? vehicle.Color : request.Color;
            vehicle.Year = request.Year ?? vehicle.Year;
            context.Vehicles.Update(vehicle);
            await context.SaveChangesAsync(cancellationToken);
            
            return GenericResponse<string>.Success("Vehicle updated", null!);
        }
    }
    
    
    
    
    
}