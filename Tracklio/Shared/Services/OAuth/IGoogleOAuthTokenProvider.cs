using System;

namespace Tracklio.Shared.Services.OAuth;

public interface IGoogleOAuthTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}
