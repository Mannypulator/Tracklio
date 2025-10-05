using System;

namespace Tracklio.Shared.Services.DVLA;

public interface IDvlaService
{
    // Define method signatures for DVLA service operations here
    Task<VehicleDetails> GetVehicleDetailsAsync(string registrationNumber, CancellationToken cancellationToken = default);
}
