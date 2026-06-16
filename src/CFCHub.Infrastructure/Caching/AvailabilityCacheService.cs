using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Scheduling;
using StackExchange.Redis;

namespace CFCHub.Infrastructure.Caching;

public interface IAvailabilityCacheService
{
    Task<IReadOnlyList<AvailableSlot>?> GetAsync(Guid instructorId, DateOnly date, CancellationToken cancellationToken = default);
    Task SetAsync(Guid instructorId, DateOnly date, IReadOnlyList<AvailableSlot> slots, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid instructorId, DateOnly date, CancellationToken cancellationToken = default);
}

public class AvailabilityCacheService : IAvailabilityCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ITenantContext _tenantContext;
    private readonly string _env;

    public AvailabilityCacheService(IConnectionMultiplexer redis, ITenantContext tenantContext)
    {
        _redis = redis;
        _tenantContext = tenantContext;
        _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    public async Task<IReadOnlyList<AvailableSlot>?> GetAsync(Guid instructorId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.InstructorAvailability(_env, _tenantContext.TenantSlug, instructorId, date);
        var val = await db.StringGetAsync(key);

        if (!val.HasValue) return null;

        return JsonSerializer.Deserialize<IReadOnlyList<AvailableSlot>>(val!);
    }

    public async Task SetAsync(Guid instructorId, DateOnly date, IReadOnlyList<AvailableSlot> slots, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.InstructorAvailability(_env, _tenantContext.TenantSlug, instructorId, date);
        var json = JsonSerializer.Serialize(slots);
        await db.StringSetAsync(key, json, TimeSpan.FromSeconds(300));
    }

    public async Task InvalidateAsync(Guid instructorId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.InstructorAvailability(_env, _tenantContext.TenantSlug, instructorId, date);
        await db.KeyDeleteAsync(key);
    }
}
