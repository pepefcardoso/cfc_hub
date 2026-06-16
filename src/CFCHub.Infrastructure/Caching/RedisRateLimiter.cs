using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using StackExchange.Redis;

namespace CFCHub.Infrastructure.Caching;

public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimiter(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<(bool Allowed, int RetryAfterSeconds)> CheckLimitAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)window.TotalMilliseconds;
        var memberId = Guid.NewGuid().ToString();

        var tran = db.CreateTransaction();
        
        // Add current request
        var addTask = tran.SortedSetAddAsync(key, memberId, now);
        // Remove older requests outside the window
        var remTask = tran.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, windowStart);
        // Count remaining elements
        var countTask = tran.SortedSetLengthAsync(key);
        // Get the oldest element to calculate exact retry after
        var oldestTask = tran.SortedSetRangeByRankWithScoresAsync(key, 0, 0);
        // Set expiry on key to avoid leaking keys
        var expireTask = tran.KeyExpireAsync(key, window);

        var committed = await tran.ExecuteAsync();
        
        if (!committed)
        {
            // If the transaction fails, we allow the request to prevent blocking due to Redis errors
            return (true, 0);
        }

        var count = await countTask;

        if (count > limit)
        {
            // Calculate how long until the oldest request in the current window falls out of the window
            var oldest = await oldestTask;
            if (oldest.Length > 0)
            {
                var oldestScore = oldest[0].Score;
                var timeRemainingMs = oldestScore + window.TotalMilliseconds - now;
                var retryAfter = (int)Math.Ceiling(Math.Max(1, timeRemainingMs / 1000.0));
                return (false, retryAfter);
            }
            return (false, (int)Math.Ceiling(window.TotalSeconds));
        }

        return (true, 0);
    }
}
