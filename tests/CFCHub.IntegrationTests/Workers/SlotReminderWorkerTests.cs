using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Scheduling;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace CFCHub.IntegrationTests.Workers;

public class SlotReminderWorkerTests : IAsyncLifetime
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
    private TestSlotReminderWorker _worker = null!;
    private Guid _tenantId = Guid.NewGuid();
    private string _tenantSlug = "test_cfc";
    private string _schemaName = "cfc_test_cfc";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));

        _redis = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());

        var services = new ServiceCollection();
        services.AddSingleton(_clock);
        
        var tenantContext = new TenantContext();
        tenantContext.Resolve(_schemaName, _tenantSlug, _tenantId);
        services.AddSingleton<ITenantContext>(tenantContext);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        var tenantRegistry = Substitute.For<ITenantRegistry>();
        tenantRegistry.GetActiveTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(new System.Collections.Generic.List<TenantRecord>
            {
                new TenantRecord(_tenantId, _tenantSlug, _schemaName, "Active")
            });
        services.AddSingleton(tenantRegistry);
        services.AddSingleton<ILogger<SlotReminderWorker>>(NullLogger<SlotReminderWorker>.Instance);

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Test");

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_schemaName}\";");
#pragma warning restore EF1002
        await _dbContext.Database.EnsureCreatedAsync();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _worker = new TestSlotReminderWorker(_redis, NullLogger<SlotReminderWorker>.Instance, scopeFactory, env);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
        _redis.Dispose();
        await _redisContainer.DisposeAsync();
    }

    private class TestSlotReminderWorker : SlotReminderWorker
    {
        public TestSlotReminderWorker(IConnectionMultiplexer redis, Microsoft.Extensions.Logging.ILogger<SlotReminderWorker> logger, IServiceScopeFactory serviceScopeFactory, IHostEnvironment env) 
            : base(redis, logger, serviceScopeFactory, env) { }

        public new Task ProcessAsync(CancellationToken ct) => base.ProcessAsync(ct);
    }

    [Fact]
    public async Task SlotReminderWorker_WithUpcomingSlot_EnqueuesReminder()
    {
        // Arrange
        var slotStartedAt = _clock.UtcNow.AddHours(24); // Exactly 24 hours ahead
        var slot = SchedulingSlot.Book(
            new SchedulingSlotId(Guid.NewGuid()),
            new InstructorId(Guid.NewGuid()),
            new VehicleId(Guid.NewGuid()),
            new TrackId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            slotStartedAt,
            CnhCategory.B,
            _clock);

        _dbContext.SchedulingSlots.Add(slot);
        await _dbContext.SaveChangesAsync();

        // Act
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert
        var updatedSlot = await _dbContext.SchedulingSlots.FirstAsync();
        updatedSlot.ReminderSentAt.Should().Be(_clock.UtcNow);

        var outboxMessage = await _dbContext.OutboxMessages.FirstOrDefaultAsync();
        outboxMessage.Should().NotBeNull();
        outboxMessage!.Type.Should().Be(nameof(SchedulingSlotReminderRequestedEvent));
    }
}
