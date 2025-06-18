using Tracklio.Shared.Domain.Dto.Users;
using Tracklio.Shared.Domain.Entities;

namespace Tracklio.Shared.Mappings;

public static partial class UserMapping
{
    public static UserResponse MapToUserResponse(this User user, int vehicleCount, int activeTicketCount)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            VehicleCount = vehicleCount,
            ActiveTicketCount = activeTicketCount
        };
    }
}