using System;
using Tracklio.Shared.Domain.Dto.Vehicle;

namespace Tracklio.Shared.Domain.Dto.Tickets;

public class TicketDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string AccountReference { get; set; } = string.Empty;
    public string VRM { get; set; } = string.Empty;
    public string VehicleMakeModel { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public DateTime IssuedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty;
}

public class TicketDetailsDto
{
    public ParkingTicketDto Ticket { get; set; } = null!;
    public List<string> Images { get; set; } = new();
    public List<TicketActionDto> Actions { get; set; } = new();
}



public class TicketActionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string User { get; set; } = string.Empty;
}
