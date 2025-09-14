using System;
using Tracklio.Shared.Domain.Dto.Vehicle;

namespace Tracklio.Shared.Domain.Dto.Admin;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Plan => Role switch
    {
        "Admin" => "Fleet",
        "Motorist" when HasSubscription => "Solo",
        "Motorist" => "Freemium",
        _ => "Freemium"
    };
    public bool HasSubscription { get; set; }
    public string AccountStatus => IsActive ? "Active" : "Inactive";
    public bool IsActive { get; set; }
    public int VehicleCount { get; set; }
    public int ActivePcnCount { get; set; }
    public string? ProfileImage { get; set; }
}


public class UserDetailsDto
{
    /// <summary>
    /// The user's basic profile information.
    /// </summary>
    public required UserDto User { get; set; }

    /// <summary>
    /// List of vehicles associated with the user.
    /// </summary>
    public List<VehicleDto> Vehicles { get; set; } = new();

    /// <summary>
    /// List of active parking tickets for the user’s vehicles.
    /// </summary>
    public List<ParkingTicketDto> Tickets { get; set; } = new();
}
