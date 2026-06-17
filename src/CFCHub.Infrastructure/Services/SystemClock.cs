using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Services;

public class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
