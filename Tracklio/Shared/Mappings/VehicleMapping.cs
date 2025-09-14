using Tracklio.Features.Vehicles;
using Tracklio.Shared.Domain.Dto.Vehicle;
using Tracklio.Shared.Domain.Entities;

namespace Tracklio.Shared.Mappings;

public static partial class VehicleMapping
{
    public static VehicleResponse MapToDto(this Vehicle entity)
    {
        return new VehicleResponse
        {
            Id = entity.Id,
            Model = entity.Model,
            ActiveTicketCount = 0,
            Color = entity.Color,
            IsActive = entity.IsActive,
            LastSyncAt = entity.LastSyncAt,
            Make = entity.Make,
            RegisteredAt = entity.CreatedAt,
            TotalOutstandingAmount = 0,
            TotalTicketCount = 0,
            UserId = entity.UserId,
            Year = entity.Year,
            VRM = entity.VRM,
        };
    }

    public static IEnumerable<VehicleResponse> MapToDto(this IEnumerable<Vehicle> entities)
    {
        return entities.Select(vehicle => vehicle.MapToDto());
    }
}