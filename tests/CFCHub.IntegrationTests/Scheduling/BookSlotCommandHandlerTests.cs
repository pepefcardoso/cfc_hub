using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Application.Scheduling.Commands.BookSlot;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Outbox;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace CFCHub.IntegrationTests.Scheduling;

public class BookSlotCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private AppDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private ISystemClock _clock = null!;
    private IIdGenerator _idGenerator = null!;
    private IConnectionMultiplexer _redis = null!;
    private RedisLockService _lockService = null!;
    private SchedulingRepository _schedulingRepository = null!;
    private OutboxService _outboxService = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.SchemaName.Returns("cfc_test");
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _idGenerator = Substitute.For<IIdGenerator>();
        _idGenerator.NewId<SchedulingSlotId>().Returns(new SchedulingSlotId(Guid.NewGuid()));

        _redis = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        _lockService = new RedisLockService(_redis, NullLogger<RedisLockService>.Instance);

        var services = new ServiceCollection();
        services.AddSingleton(_tenantContext);
        services.AddSingleton(_clock);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        var serviceProvider = services.BuildServiceProvider();
        _dbContext = serviceProvider.GetRequiredService<AppDbContext>();

#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_tenantContext.SchemaName}\";");
#pragma warning restore EF1002
        await _dbContext.Database.EnsureCreatedAsync();

        _schedulingRepository = new SchedulingRepository(_dbContext);
        _outboxService = new OutboxService(_dbContext, _clock);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
        _redis.Dispose();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task BookSlot_ConcurrentRequests_OnlyOneSucceeds()
    {
        // Arrange
        var command = new BookSlotCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CnhCategory.B,
            _clock.UtcNow.AddDays(1)
        );

        int concurrentRequests = 10;
        var tasks = new Task<Result<BookSlotResult>>[concurrentRequests];

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                // Each request needs its own scoped DbContext and handler to simulate concurrent HTTP requests properly
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_dbContainer.GetConnectionString());
                using var dbContext = new AppDbContext(optionsBuilder.Options, _tenantContext, _clock);
                var repo = new SchedulingRepository(dbContext);
                var outbox = new OutboxService(dbContext, _clock);
                
                var idGenerator = Substitute.For<IIdGenerator>();
                idGenerator.NewId<SchedulingSlotId>().Returns(new SchedulingSlotId(Guid.NewGuid()));

                var handler = new BookSlotCommandHandler(
                    _lockService,
                    repo,
                    dbContext, // IUnitOfWork
                    outbox,
                    idGenerator,
                    _clock);

                return await handler.Handle(command, CancellationToken.None);
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        var failedResults = results.Where(r => !r.IsSuccess).ToList();

        successfulResults.Should().ContainSingle();
        failedResults.Should().HaveCount(concurrentRequests - 1);
        failedResults.All(r => r.Error != null && (r.Error.Code == "SLOT_LOCK_FAILED" || r.Error.Code == "SLOT_OVERLAP")).Should().BeTrue();

        // Verify exactly one slot in DB
        var slotsInDb = await _dbContext.SchedulingSlots.ToListAsync();
        slotsInDb.Should().ContainSingle();

        // Verify exactly one outbox message
        var outboxInDb = await _dbContext.OutboxMessages.ToListAsync();
        outboxInDb.Should().ContainSingle();
        outboxInDb.First().Type.Should().Be("SlotBooked");
    }

    [Fact]
    public async Task BookSlot_VerifiesExclusionConstraint_OnSimultaneousConcurrentInserts()
    {
        // Arrange
        var command = new BookSlotCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CnhCategory.B,
            _clock.UtcNow.AddDays(1)
        );

        // We simulate a race condition bypassing Redis locks to test PostgreSQL exclusion constraint/transaction isolation
        var mockLockService = Substitute.For<ISchedulingLockService>();
        mockLockService.AcquireAllAsync(Arg.Any<System.Collections.Generic.IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(true); // Always acquire lock

        int concurrentRequests = 3;
        var tasks = new Task<Result<BookSlotResult>>[concurrentRequests];

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_dbContainer.GetConnectionString());
                using var dbContext = new AppDbContext(optionsBuilder.Options, _tenantContext, _clock);
                var repo = new SchedulingRepository(dbContext);
                var outbox = new OutboxService(dbContext, _clock);
                
                var idGenerator = Substitute.For<IIdGenerator>();
                idGenerator.NewId<SchedulingSlotId>().Returns(new SchedulingSlotId(Guid.NewGuid()));

                var handler = new BookSlotCommandHandler(
                    mockLockService, // Bypassed Redis lock
                    repo,
                    dbContext, // IUnitOfWork
                    outbox,
                    idGenerator,
                    _clock);

                try 
                {
                    return await handler.Handle(command, CancellationToken.None);
                }
                catch (DbUpdateException ex)
                {
                    // EF Core might throw DbUpdateException if the exclusion constraint fails directly on insert
                    return Result<BookSlotResult>.Failure(Error.Conflict("DB_CONSTRAINT", ex.Message));
                }
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        var failedResults = results.Where(r => !r.IsSuccess).ToList();

        successfulResults.Should().ContainSingle();
        
        // Either they failed by our manual `FOR UPDATE` check, or by DB constraint if they squeezed past
        failedResults.Should().HaveCount(concurrentRequests - 1);
        
        var slotsInDb = await _dbContext.SchedulingSlots.ToListAsync();
        slotsInDb.Should().ContainSingle();
    }
}
