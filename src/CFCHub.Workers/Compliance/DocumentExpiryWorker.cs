using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CFCHub.Workers.Compliance;

public class DocumentExpiryWorker : LeasedBackgroundService
{
    private readonly IHostEnvironment _env;
    private readonly ISystemClock _clock;

    public DocumentExpiryWorker(
        IConnectionMultiplexer redis,
        ILogger<DocumentExpiryWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IHostEnvironment env,
        ISystemClock clock)
        : base(redis, logger, serviceScopeFactory)
    {
        _env = env;
        _clock = clock;
    }

    protected override string LeaseKey => RedisKeys.DocExpiryLease(_env.EnvironmentName, _clock.UtcNow.ToString("yyyy-MM-dd"));
    protected override TimeSpan LeaseTtl => TimeSpan.FromHours(24);
    protected override TimeSpan PollingInterval => TimeSpan.FromHours(1);

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        if (now.Hour != 6)
        {
            // Only run near 06:00 UTC
            return;
        }

        using var scope = ServiceScopeFactory.CreateScope();
        var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var activeTenants = await tenantRegistry.GetActiveTenantsAsync(ct);

        foreach (var tenant in activeTenants)
        {
            await ProcessTenantDocumentsAsync(tenant, ct);
        }
    }

    private async Task ProcessTenantDocumentsAsync(TenantRecord tenant, CancellationToken ct)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.Resolve(tenant.SchemaName, tenant.Slug, tenant.Id);
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DocumentExpiryWorker>>();

        var today = DateOnly.FromDateTime(_clock.UtcNow.Date);
        var toDate = today.AddDays(30);

        var expiringDocs = await documentRepository.GetExpiringAsync(today, toDate, ct);

        foreach (var doc in expiringDocs)
        {
            if (doc.LastAlertSentAt.HasValue && (_clock.UtcNow - doc.LastAlertSentAt.Value).TotalHours < 24)
            {
                continue;
            }

            var daysUntilExpiry = doc.ExpiryDate.DayNumber - today.DayNumber;
            AlertTier? tier = daysUntilExpiry switch
            {
                <= 1 => AlertTier.D1,
                <= 7 => AlertTier.D7,
                <= 15 => AlertTier.D15,
                <= 30 => AlertTier.D30,
                _ => null
            };

            if (tier.HasValue)
            {
                doc.MarkAlertSent(tier.Value, _clock);
                logger.LogInformation("Marked alert sent for document {DocId} tier {Tier} tenant {TenantSlug}", doc.Id.Value, tier.Value, tenant.Slug);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
