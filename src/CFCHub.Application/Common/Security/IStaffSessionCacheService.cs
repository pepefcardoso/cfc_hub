using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Common.Security;

public interface IStaffSessionCacheService
{
    Task CacheSessionAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default);
}
