using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Common.Interfaces;

public interface IRateLimiter
{
    Task<(bool Allowed, int RetryAfterSeconds)> CheckLimitAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
}
