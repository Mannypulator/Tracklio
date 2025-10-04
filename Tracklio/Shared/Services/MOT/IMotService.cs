using System;

namespace Tracklio.Shared.Services.MOT;

public interface IMotService
{
    Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken = default);
    Task<VehicleMotHistory> GetVehicleHistoryByRegistrationAsync(string registration, CancellationToken cancellationToken = default);
    Task<VehicleMotHistory> GetVehicleHistoryByVinAsync(string vin, CancellationToken cancellationToken = default);
    Task<BulkDownloadResponse> GetBulkDownloadLinksAsync(CancellationToken cancellationToken = default);
}
