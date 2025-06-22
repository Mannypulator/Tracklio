using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Dto;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;
using Tracklio.Shared.Persistence;
using Tracklio.Shared.Security;
using Tracklio.Shared.Slices;

namespace Tracklio.Features.Vehicles;

public sealed class AddUserVehicle : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("api/v1/vehicles", async (
                ClaimsPrincipal claims,
               [FromBody] AddVehicleCommand request,
                [FromServices] IMediator mediator,
                CancellationToken ct
            ) =>
            {
                var userId = claims.GetUserIdAsGuid();
                request.UserId = userId;
                var response = await mediator.Send(request, ct);
                return response.ReturnedResponse();
            })
            .WithName("AddUserVehicle")
            .WithTags("Vehicles")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Endpoint to add a vehicle",
                Description =
                    "Allows user to add their vehicle.",
                OperationId = "AddUserVehicle",
            })
            .Produces<GenericResponse<string>>(StatusCodes.Status200OK)
            .Produces<GenericResponse<string>>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PoliciesConstant.MotoristOrAdmin);
    }

    public class AddVehicleCommand : IRequest<GenericResponse<string>>
    {
        [JsonIgnore]
        public Guid UserId { get; set; }
        public string VRM { get; set; } = string.Empty;
        
        public string? Make { get; set; }
        
        public string? Model { get; set; }
        
        public string? Color { get; set; }
        
        public int? Year { get; set; }
    }

    public class AddVehicleCommandValidator : AbstractValidator<AddVehicleCommand>
    {
        public AddVehicleCommandValidator()
        {
            RuleFor(x => x.VRM)
                .NotEmpty()
                .WithMessage("Vehicle registration number (VRM) is required")
                .Length(2, 10)
                .WithMessage("VRM must be between 2 and 10 characters")
                .Matches(@"^[A-Z0-9\s]+$")
                .WithMessage("VRM can only contain letters, numbers and spaces")
                .Must(BeValidUKVRM)
                .WithMessage("Invalid UK vehicle registration format");

            RuleFor(x => x.Make)
                .MaximumLength(50)
                .WithMessage("Make must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\s\-&]+$")
                .WithMessage("Make can only contain letters, numbers, spaces, hyphens and ampersands");

            RuleFor(x => x.Model)
                .MaximumLength(50)
                .WithMessage("Model must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\s\-&().]+$")
                .WithMessage("Model can only contain letters, numbers, spaces, hyphens, ampersands and brackets");

            RuleFor(x => x.Color)
                .MaximumLength(30)
                .WithMessage("Color must not exceed 30 characters")
                .Matches(@"^[a-zA-Z\s\-]+$")
                .WithMessage("Color can only contain letters, spaces and hyphens");

            RuleFor(x => x.Year)
                .GreaterThanOrEqualTo(1900)
                .WithMessage("Year must be 1900 or later")
                .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
                .WithMessage($"Year must not exceed {DateTime.UtcNow.Year + 1}");
        }
        
        private bool BeValidUKVRM(string vrm)
        {
            if (string.IsNullOrEmpty(vrm))
                return false;
            
            vrm = vrm.Replace(" ", "").ToUpperInvariant();
            
            var patterns = new[]
            {
                @"^[A-Z]{2}[0-9]{2}[A-Z]{3}$",     
                @"^[A-Z][0-9]{1,3}[A-Z]{3}$",      
                @"^[A-Z]{3}[0-9]{1,3}[A-Z]$",      
                @"^[0-9]{1,4}[A-Z]{1,3}$",         
                @"^[A-Z]{1,3}[0-9]{1,4}$"  
            };

            return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(vrm, pattern));
        }
    }

    public class AddVehicleCommandHandler(RepositoryContext context) : IRequestHandler<AddVehicleCommand, GenericResponse<string>>
    {
        public async Task<GenericResponse<string>> Handle(AddVehicleCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return GenericResponse<string>.Error(401, "Unauthorized");
            }

            if (await context.Vehicles.AnyAsync(x => x.VRM.Trim() == request.VRM.Trim(), cancellationToken))
            {
                return GenericResponse<string>.Error(409, $"Vehicle with {request.VRM} already exists");
            }

            await context.Vehicles.AddAsync(request.MapToEntity(), cancellationToken);
            
            await context.SaveChangesAsync(cancellationToken);
            
            return GenericResponse<string>.Success("Vehicle added", null!);
            

        }
    }
}

public static partial class VehicleMapping
{
    public static Vehicle MapToEntity(this AddUserVehicle.AddVehicleCommand dto)
    {
        return new Vehicle()
        {
            UserId = dto.UserId,
            Id = Guid.NewGuid(),
            VRM = dto.VRM,
            Color = dto.Color,
            Make = dto.Make,
            IsActive = true,
            Model = dto.Model,
            RegisteredAt = DateTime.UtcNow,
            Year = dto.Year
        };
    }
}

