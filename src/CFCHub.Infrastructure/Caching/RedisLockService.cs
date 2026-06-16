using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Scheduling;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CFCHub.Infrastructure.Caching;

public sealed class RedisLockService : ISchedulingLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisLockService> _logger;

    public const int MaxLockTtlSeconds = 30;

    public RedisLockService(IConnectionMultiplexer redis, ILogger<RedisLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(string key, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        return await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(MaxLockTtlSeconds), When.NotExists);
    }

    public Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        if (ttl.TotalSeconds != MaxLockTtlSeconds)
        {
            throw new ArgumentException($"TTL must be exactly {MaxLockTtlSeconds} seconds.", nameof(ttl));
        }
        
        return TryAcquireAsync(key, ct);
    }

    public async Task ReleaseAsync(string key, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var wasDeleted = await db.KeyDeleteAsync(key);
        
        if (!wasDeleted)
        {
            _logger.LogDebug("Lock key {Key} was already expired or did not exist when trying to release", key);
        }
    }

    public async Task<bool> AcquireAllAsync(IEnumerable<string> keys, CancellationToken ct)
    {
        var sortedKeys = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var acquiredKeys = new List<string>(sortedKeys.Count);

        foreach (var key in sortedKeys)
        {
            var acquired = await TryAcquireAsync(key, ct);
            if (acquired)
            {
                acquiredKeys.Add(key);
            }
            else
            {
                foreach (var acquiredKey in acquiredKeys)
                {
                    await ReleaseAsync(acquiredKey, ct);
                }
                return false;
            }
        }

        return true;
    }

    public async Task ReleaseAllAsync(IEnumerable<string> keys, CancellationToken ct)
    {
        foreach (var key in keys)
        {
            await ReleaseAsync(key, ct);
        }
    }
}
