using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CFCHub.Workers.Outbox;

public class OutboxWorker : LeasedBackgroundService
{
    private readonly IHostEnvironment _env;

    public OutboxWorker(
        IConnectionMultiplexer redis,
        ILogger<OutboxWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IHostEnvironment env)
        : base(redis, logger, serviceScopeFactory)
    {
        _env = env;
    }

    protected override string LeaseKey => RedisKeys.OutboxWorkerLease(_env.EnvironmentName, "global");
    protected override TimeSpan LeaseTtl => TimeSpan.FromSeconds(60);
    protected override TimeSpan PollingInterval => TimeSpan.FromSeconds(5);

    protected override async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var activeTenants = await tenantRegistry.GetActiveTenantsAsync(ct);

        foreach (var tenant in activeTenants)
        {
            await ProcessTenantOutboxAsync(tenant, ct);
        }
    }

    private async Task ProcessTenantOutboxAsync(TenantRecord tenant, CancellationToken ct)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        if (tenantContext is TenantContext concreteContext)
        {
            concreteContext.Resolve(tenant.SchemaName, tenant.Slug, tenant.Id);
        }
        else
        {
            throw new InvalidOperationException("ITenantContext is not a TenantContext");
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OutboxWorker>>();
        var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();

        // Wrap in transaction to hold the FOR UPDATE locks
        using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

#pragma warning disable EF1002
        var pendingMessageIds = await dbContext.Database
            .SqlQueryRaw<Guid>($"SELECT id FROM {tenant.SchemaName}.outbox_messages WHERE status = 'Pending' AND scheduled_after <= NOW() ORDER BY created_at ASC LIMIT 10 FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);
#pragma warning restore EF1002

        if (!pendingMessageIds.Any())
        {
            await transaction.RollbackAsync(ct);
            return;
        }

        // Load entities
        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => pendingMessageIds.Contains(m.Id.Value))
            .ToListAsync(ct);

        foreach (var message in outboxMessages)
        {
            message.MarkAsProcessing();
            await dbContext.SaveChangesAsync(ct);

            try
            {
                await dispatcher.DispatchAsync(message, ct);
                message.MarkAsProcessed(clock.UtcNow);
                logger.LogInformation("Processed outbox message {MessageId} of type {MessageType} for tenant {TenantSlug}", message.Id.Value, message.Type, tenant.Slug);
            }
            catch (Exception ex)
            {
                message.MarkAsFailed(clock.UtcNow, ex.Message, ex.ToString());
                if (message.Status == OutboxMessageStatus.Failed)
                {
                    logger.LogCritical(ex, "Outbox message {MessageId} of type {MessageType} failed permanently for tenant {TenantSlug}.", message.Id.Value, message.Type, tenant.Slug);
                }
                else
                {
                    logger.LogError(ex, "Error processing outbox message {MessageId} of type {MessageType} for tenant {TenantSlug}. Retrying later.", message.Id.Value, message.Type, tenant.Slug);
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }
}
