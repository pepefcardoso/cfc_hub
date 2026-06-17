using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CFCHub.Workers.Common;

public abstract class LeasedBackgroundService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger _logger;
    protected readonly IServiceScopeFactory ServiceScopeFactory;

    protected LeasedBackgroundService(
        IConnectionMultiplexer redis,
        ILogger logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _redis = redis;
        _logger = logger;
        ServiceScopeFactory = serviceScopeFactory;
    }

    protected abstract string LeaseKey { get; }
    protected abstract TimeSpan LeaseTtl { get; }
    protected abstract TimeSpan PollingInterval { get; }
    protected abstract Task ProcessAsync(CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            bool leaseAcquired = false;
            try
            {
                leaseAcquired = await db.StringSetAsync(
                    LeaseKey, 
                    Environment.MachineName, 
                    LeaseTtl, 
                    When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {Worker} failed to connect to Redis while acquiring lease {LeaseKey}.", GetType().Name, LeaseKey);
            }

            if (leaseAcquired)
            {
                _logger.LogDebug("Worker {Worker} acquired lease for {LeaseKey}. Starting cycle.", GetType().Name, LeaseKey);

                try   
                { 
                    await ProcessAsync(stoppingToken); 
                }
                catch (Exception ex) 
                { 
                    _logger.LogError(ex, "Worker {Worker} failed.", GetType().Name); 
                }
                finally 
                { 
                    await db.KeyDeleteAsync(LeaseKey); 
                    _logger.LogDebug("Worker {Worker} released lease for {LeaseKey}.", GetType().Name, LeaseKey);
                }
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Task.Delay was cancelled due to stoppingToken
                break;
            }
        }
    }
}
