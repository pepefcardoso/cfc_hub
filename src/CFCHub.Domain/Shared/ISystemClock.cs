using System;

namespace CFCHub.Domain.Shared;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
