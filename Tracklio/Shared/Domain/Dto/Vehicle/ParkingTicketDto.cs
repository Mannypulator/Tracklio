using System;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Domain.Dto.Vehicle;

public class ParkingTicketDto
{
    public Guid Id { get; set; }
    public string PCNReference { get; set; } = string.Empty;

    public string VRM { get; set; } = string.Empty;

    public DateTime IssuedDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal? DiscountedAmount { get; set; }

    public DateTime PaymentDeadline { get; set; }

    public DateTime? AppealDeadline { get; set; }

    public string Status { get; set; } = TicketStatus.Active.ToString();


    public string IssuingAuthority { get; set; } = string.Empty;

    public string? PaymentUrl { get; set; }

    public string? AppealUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? LastNotificationSent { get; set; }

    public string? ExternalTicketId { get; set; }


    public string? DataProvider { get; set; }
}


public class VehicleDto
{
    public Guid Id { get; set; }
    public string VRM { get; set; } = string.Empty;

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? Color { get; set; }

    public int? Year { get; set; }

    public DateTime RegisteredAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastSyncAt { get; set; }
}
