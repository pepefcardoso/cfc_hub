using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Domain.Scheduling;

public interface ISchedulingLockService
{
    Task<bool> TryAcquireAsync(string key, CancellationToken ct);
    Task ReleaseAsync(string key, CancellationToken ct);
    Task<bool> AcquireAllAsync(IEnumerable<string> keys, CancellationToken ct);
}
