using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
using CFCHub.Infrastructure.Persistence;
using CFCHub.IntegrationTests.Common;
using CFCHub.Workers.Outbox;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;
using Xunit;

namespace CFCHub.IntegrationTests.Outbox;

[Collection("Integration")]
public class OutboxWorkerTests : IntegrationTestBase
{
    private ISystemClock _clock = null!;
    private IOutboxMessageDispatcher _dispatcher = null!;
    private TestOutboxWorker _worker = null!;
    private DateTimeOffset _initialTime;

    public OutboxWorkerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        _clock = Substitute.For<ISystemClock>();
        _initialTime = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(_initialTime);

        _dispatcher = Substitute.For<IOutboxMessageDispatcher>();

        var tenantRegistry = Substitute.For<ITenantRegistry>();
        tenantRegistry.GetActiveTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TenantRecord>
            {
                new TenantRecord(TenantId, "test_slug", TenantSchema, "Active")
            });

        var customizedFactory = _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<ITenantContext>(_ => new LocalTestTenantContext(TenantId, TenantSchema));
                services.AddSingleton(_clock);
                services.AddSingleton(_dispatcher);
                services.AddSingleton(tenantRegistry);
            });
        });

        Client = customizedFactory.CreateClient();
        Scope = customizedFactory.Services.CreateScope();
        ServiceProvider = Scope.ServiceProvider;
        Sender = ServiceProvider.GetRequiredService<MediatR.ISender>();
        DbContext = ServiceProvider.GetRequiredService<AppDbContext>();

        // Create schema and run migrations for this test instance
#pragma warning disable EF1003
        await DbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA " + TenantSchema);
#pragma warning restore EF1003
        await DbContext.Database.MigrateAsync();

        var redis = ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Test");

        var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        _worker = new TestOutboxWorker(redis, NullLogger<OutboxWorker>.Instance, scopeFactory, env);
    }

    [Fact]
    public async Task OutboxWorker_ProcessMessage_MarksAsProcessed()
    {
        // Arrange
        var message = OutboxMessage.Create("TestType", "{}", _initialTime);
        DbContext.OutboxMessages.Add(message);
        await DbContext.SaveChangesAsync();

        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert
        var updatedMessage = await DbContext.OutboxMessages.FirstAsync();
        updatedMessage.Status.Should().Be(OutboxMessageStatus.Processed);
        updatedMessage.ProcessedAt.Should().Be(_initialTime);
        await _dispatcher.Received(1).DispatchAsync(Arg.Is<OutboxMessage>(m => m.Id == message.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxWorker_OnHandlerException_RetriesWithBackoff()
    {
        // Arrange
        var message = OutboxMessage.Create("TestType", "{}", _initialTime);
        DbContext.OutboxMessages.Add(message);
        await DbContext.SaveChangesAsync();

        // Simulate crash
        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Handler crashed"));

        // Act 1 - First processing
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert 1
        var msgAfter1 = await DbContext.OutboxMessages.FirstAsync();
        msgAfter1.Attempts.Should().Be(1);
        msgAfter1.Status.Should().Be(OutboxMessageStatus.Pending);
        msgAfter1.ScheduledAfter.Should().Be(_initialTime.AddSeconds(2)); // 2^1 backoff

        // Act 2 - Try immediately (should skip because scheduled_after is in future)
        await _worker.ProcessAsync(CancellationToken.None);
        var msgAfter2 = await DbContext.OutboxMessages.FirstAsync();
        msgAfter2.Attempts.Should().Be(1); // Unchanged

        // Act 3 - Move time forward and process again
        _clock.UtcNow.Returns(_initialTime.AddSeconds(3));
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert 3
        var msgAfter3 = await DbContext.OutboxMessages.FirstAsync();
        msgAfter3.Attempts.Should().Be(2);
        msgAfter3.Status.Should().Be(OutboxMessageStatus.Pending);
        msgAfter3.ScheduledAfter.Should().Be(_initialTime.AddSeconds(3).AddSeconds(4)); // 2^2 backoff
        
        // Act 4 - Move time forward and succeed
        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _clock.UtcNow.Returns(_initialTime.AddSeconds(10));
        await _worker.ProcessAsync(CancellationToken.None);
        
        var finalMsg = await DbContext.OutboxMessages.FirstAsync();
        finalMsg.Attempts.Should().Be(2); // still 2, hasn't incremented on success
        finalMsg.Status.Should().Be(OutboxMessageStatus.Processed);
    }

    [Fact]
    public async Task OutboxWorker_OnMaxAttemptsReached_MarksFailed()
    {
        // Arrange
        var message = OutboxMessage.Create("TestType", "{}", _initialTime);
        DbContext.OutboxMessages.Add(message);
        await DbContext.SaveChangesAsync();

        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Permanent failure"));

        // Act
        for (int i = 0; i < 5; i++)
        {
            // Move time forward by a large amount to ensure we bypass backoff
            _clock.UtcNow.Returns(_initialTime.AddDays(i));
            await _worker.ProcessAsync(CancellationToken.None);
        }

        // Assert
        var finalMsg = await DbContext.OutboxMessages.FirstAsync();
        finalMsg.Attempts.Should().Be(5);
        finalMsg.Status.Should().Be(OutboxMessageStatus.Failed);
    }

    [Fact]
    public async Task OutboxWorker_ForUpdateSkipLocked_NoDuplicateProcessing()
    {
        // Arrange
        var message = OutboxMessage.Create("TestType", "{}", _initialTime);
        DbContext.OutboxMessages.Add(message);
        await DbContext.SaveChangesAsync();

        // We simulate a slow dispatcher so both workers can overlap
        var tcs = new TaskCompletionSource();
        _dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(async (info) =>
            {
                await tcs.Task; // block here until we let it go
            });

        // Create a second worker instance
        var redis = ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Test");
        var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var worker2 = new TestOutboxWorker(redis, NullLogger<OutboxWorker>.Instance, scopeFactory, env);

        // Act
        // Start both workers concurrently
        var task1 = _worker.ProcessAsync(CancellationToken.None);
        var task2 = worker2.ProcessAsync(CancellationToken.None);

        // Let them reach the dispatch phase
        await Task.Delay(100);

        // Unblock them
        tcs.SetResult();

        await Task.WhenAll(task1, task2);

        // Assert
        var finalMsg = await DbContext.OutboxMessages.FirstAsync();
        finalMsg.Status.Should().Be(OutboxMessageStatus.Processed);
        
        // Because of FOR UPDATE SKIP LOCKED, only ONE worker should have grabbed the row
        // So the dispatcher should only be called once
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }

    private class TestOutboxWorker : OutboxWorker
    {
        public TestOutboxWorker(
            IConnectionMultiplexer redis,
            Microsoft.Extensions.Logging.ILogger<OutboxWorker> logger,
            IServiceScopeFactory serviceScopeFactory,
            IHostEnvironment env)
            : base(redis, logger, serviceScopeFactory, env)
        {
        }

        public new Task ProcessAsync(CancellationToken ct) => base.ProcessAsync(ct);
    }

    private class LocalTestTenantContext : ITenantContext
    {
        public LocalTestTenantContext(Guid tenantId, string schemaName)
        {
            TenantId = tenantId;
            SchemaName = schemaName;
            TenantSlug = schemaName.Replace("cfc_", "");
        }

        public Guid TenantId { get; }
        public string TenantSlug { get; }
        public string SchemaName { get; }
        public bool IsResolved => true;
    }
}
