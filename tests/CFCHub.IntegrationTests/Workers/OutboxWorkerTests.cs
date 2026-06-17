using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Outbox;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace CFCHub.IntegrationTests.Workers;

public class OutboxWorkerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private ISystemClock _clock = null!;
    private IConnectionMultiplexer _redis = null!;
    private IServiceProvider _serviceProvider = null!;
    private IOutboxMessageDispatcher _dispatcher = null!;
    private TestOutboxWorker _worker = null!;
    private Guid _tenantId = Guid.NewGuid();
    private string _tenantSlug = "test_cfc";
    private string _schemaName = "cfc_test_cfc";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _redis = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());

        var services = new ServiceCollection();
        services.AddSingleton(_clock);
        
        // Setup Tenant Context
        var tenantContext = new TenantContext();
        tenantContext.Resolve(_schemaName, _tenantSlug, _tenantId);
        services.AddSingleton<ITenantContext>(tenantContext);

        // Setup DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        // Setup mocked dispatcher
        _dispatcher = Substitute.For<IOutboxMessageDispatcher>();
        services.AddSingleton(_dispatcher);

        // Setup mocked tenant registry
        var tenantRegistry = Substitute.For<ITenantRegistry>();
        tenantRegistry.GetActiveTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(new System.Collections.Generic.List<TenantRecord>
            {
                new TenantRecord(_tenantId, _tenantSlug, _schemaName, "Active")
            });
        services.AddSingleton(tenantRegistry);
        services.AddSingleton<ILogger<OutboxWorker>>(NullLogger<OutboxWorker>.Instance);

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Test");

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

        // Init DB Schema
#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_schemaName}\";");
#pragma warning restore EF1002
        await _dbContext.Database.EnsureCreatedAsync();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _worker = new TestOutboxWorker(_redis, NullLogger<OutboxWorker>.Instance, scopeFactory, env);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
        _redis.Dispose();
        await _redisContainer.DisposeAsync();
    }

    private class TestOutboxWorker : OutboxWorker
    {
        public TestOutboxWorker(IConnectionMultiplexer redis, Microsoft.Extensions.Logging.ILogger<OutboxWorker> logger, IServiceScopeFactory serviceScopeFactory, IHostEnvironment env) 
            : base(redis, logger, serviceScopeFactory, env) { }

        public new Task ProcessAsync(CancellationToken ct) => base.ProcessAsync(ct);
    }

    [Fact]
    public async Task OutboxWorker_OnHandlerCrash_RetriesWithBackoff()
    {
        // Arrange
        var initialTime = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(initialTime);

        var message = OutboxMessage.Create("TestType", "{}", initialTime);
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        // Simulate crash
        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Handler crashed"));

        // Act 1 - First processing
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert 1
        var msgAfter1 = await _dbContext.OutboxMessages.FirstAsync();
        msgAfter1.Attempts.Should().Be(1);
        msgAfter1.Status.Should().Be(OutboxMessageStatus.Pending);
        msgAfter1.ScheduledAfter.Should().Be(initialTime.AddSeconds(2)); // 2^1 backoff

        // Act 2 - Try immediately (should skip because scheduled_after is in future)
        await _worker.ProcessAsync(CancellationToken.None);
        var msgAfter2 = await _dbContext.OutboxMessages.FirstAsync();
        msgAfter2.Attempts.Should().Be(1); // Unchanged

        // Act 3 - Move time forward and process again
        _clock.UtcNow.Returns(initialTime.AddSeconds(3));
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert 3
        var msgAfter3 = await _dbContext.OutboxMessages.FirstAsync();
        msgAfter3.Attempts.Should().Be(2);
        msgAfter3.Status.Should().Be(OutboxMessageStatus.Pending);
        msgAfter3.ScheduledAfter.Should().Be(initialTime.AddSeconds(3).AddSeconds(4)); // 2^2 backoff
    }

    [Fact]
    public async Task OutboxWorker_AtomicityTest_CrashBetweenWriteAndDispatch_MessageIsRetried()
    {
        // Arrange
        var initialTime = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(initialTime);

        var message = OutboxMessage.Create("TestType", "{}", initialTime);
        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        // Simulate a crash during dispatch
        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Simulated crash during dispatch"));

        // Act
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert
        var updatedMessage = await _dbContext.OutboxMessages.FirstAsync();
        
        // Message is back to Pending, attempt count incremented, error recorded
        updatedMessage.Status.Should().Be(OutboxMessageStatus.Pending);
        updatedMessage.Attempts.Should().Be(1);
        updatedMessage.Error.Should().Be("Simulated crash during dispatch");

        // Now fix the dispatcher and move time forward
        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _clock.UtcNow.Returns(initialTime.AddSeconds(10));

        // Act 2
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert 2
        var finalMessage = await _dbContext.OutboxMessages.FirstAsync();
        finalMessage.Status.Should().Be(OutboxMessageStatus.Processed);
        finalMessage.ProcessedAt.Should().Be(initialTime.AddSeconds(10));
    }
}
