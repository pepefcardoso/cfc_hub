using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CFCHub.Workers.Scheduling;

public class SlotReminderWorker : LeasedBackgroundService
{
    private readonly IHostEnvironment _env;

    public SlotReminderWorker(
        IConnectionMultiplexer redis,
        ILogger<SlotReminderWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IHostEnvironment env)
        : base(redis, logger, serviceScopeFactory)
    {
        _env = env;
    }

    protected override string LeaseKey => RedisKeys.SlotReminderLease(_env.EnvironmentName, "global");
    protected override TimeSpan LeaseTtl => TimeSpan.FromHours(1);
    protected override TimeSpan PollingInterval => TimeSpan.FromMinutes(30);

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var activeTenants = await tenantRegistry.GetActiveTenantsAsync(ct);

        foreach (var tenant in activeTenants)
        {
            await ProcessTenantSlotsAsync(tenant, ct);
        }
    }

    private async Task ProcessTenantSlotsAsync(TenantRecord tenant, CancellationToken ct)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.Resolve(tenant.SchemaName, tenant.Slug, tenant.Id);
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SlotReminderWorker>>();

        var now = clock.UtcNow;
        var startWindow = now.AddHours(23);
        var endWindow = now.AddHours(25);

        var upcomingSlots = await dbContext.SchedulingSlots
            .Where(s => s.StartedAt >= startWindow && s.StartedAt <= endWindow)
            .Where(s => s.Status == CFCHub.Domain.Scheduling.SlotStatus.Confirmed)
            .Where(s => s.ReminderSentAt == null)
            .ToListAsync(ct);

        foreach (var slot in upcomingSlots)
        {
            slot.MarkReminderSent(clock);
            logger.LogInformation("Marked reminder sent for slot {SlotId} tenant {TenantSlug}", slot.Id.Value, tenant.Slug);
        }

        if (upcomingSlots.Any())
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
