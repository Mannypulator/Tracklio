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
            ActiveTicketCount = entity.ParkingTickets?.Count(t => t.Status == Domain.Enums.TicketStatus.Active) ?? 0,
            Color = entity.Color,
            IsActive = entity.IsActive,
            LastSyncAt = entity.LastSyncAt,
            Make = entity.Make,
            RegisteredAt = entity.CreatedAt,
            TotalOutstandingAmount = entity.ParkingTickets?.Sum(t => t.Amount - t.DiscountedAmount) ?? 0,
            TotalTicketCount = entity.ParkingTickets?.Count ?? 0,
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