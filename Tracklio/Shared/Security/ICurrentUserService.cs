using System;

namespace Tracklio.Shared.Security;

public interface ICurrentUserService
{
    string? UserId { get; }
}
