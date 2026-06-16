using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace CFCHub.Infrastructure.Caching;

public record TenantCacheItem(string SchemaName, string TenantSlug, Guid TenantId);

public interface ITenantCacheService
{
    Task<TenantCacheItem?> GetAsync(string slug, CancellationToken cancellationToken = default);
    Task SetAsync(string slug, TenantCacheItem tenantContext, CancellationToken cancellationToken = default);
}

public class TenantCacheService : ITenantCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _env;

    public TenantCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    public async Task<TenantCacheItem?> GetAsync(string slug, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TenantResolution(_env, slug);
        var val = await db.StringGetAsync(key);

        if (!val.HasValue) return null;

        return JsonSerializer.Deserialize<TenantCacheItem>(val.ToString());
    }

    public async Task SetAsync(string slug, TenantCacheItem tenantContext, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TenantResolution(_env, slug);
        var json = JsonSerializer.Serialize(tenantContext);
        await db.StringSetAsync(key, json, TimeSpan.FromSeconds(300));
    }
}
