using System;

namespace Tracklio.Shared.Domain.Dto.Admin;

public class DashboardSummaryDto
{
    public int TotalUsers { get; set; }
    public int TotalVehicles { get; set; }
    public int TotalTickets { get; set; }
    public int SubscribedUsers { get; set; }
    public double UserGrowthRate { get; set; }
    public double VehicleGrowthRate { get; set; }
    public double TicketGrowthRate { get; set; }
    public double SubscriptionGrowthRate { get; set; }
}
