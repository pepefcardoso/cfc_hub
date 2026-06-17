using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Compliance.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
using CFCHub.Domain.Students;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Compliance;
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

public class DocumentExpiryWorkerTests : IAsyncLifetime
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
    private TestDocumentExpiryWorker _worker = null!;
    private Guid _tenantId = Guid.NewGuid();
    private string _tenantSlug = "test_cfc";
    private string _schemaName = "cfc_test_cfc";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 10, 10, 6, 0, 0, TimeSpan.Zero)); // 06:00 UTC

        _redis = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());

        var services = new ServiceCollection();
        services.AddSingleton(_clock);
        
        var tenantContext = new TenantContext();
        tenantContext.Resolve(_schemaName, _tenantSlug, _tenantId);
        services.AddSingleton<ITenantContext>(tenantContext);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        services.AddScoped<IDocumentRepository, CFCHub.Infrastructure.Persistence.Repositories.DocumentRepository>();

        var tenantRegistry = Substitute.For<ITenantRegistry>();
        tenantRegistry.GetActiveTenantsAsync(Arg.Any<CancellationToken>())
            .Returns(new System.Collections.Generic.List<TenantRecord>
            {
                new TenantRecord(_tenantId, _tenantSlug, _schemaName, "Active")
            });
        services.AddSingleton(tenantRegistry);
        services.AddSingleton<ILogger<DocumentExpiryWorker>>(NullLogger<DocumentExpiryWorker>.Instance);

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Test");

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_schemaName}\";");
#pragma warning restore EF1002
        await _dbContext.Database.EnsureCreatedAsync();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _worker = new TestDocumentExpiryWorker(_redis, NullLogger<DocumentExpiryWorker>.Instance, scopeFactory, env, _clock);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
        _redis.Dispose();
        await _redisContainer.DisposeAsync();
    }

    private class TestDocumentExpiryWorker : DocumentExpiryWorker
    {
        public TestDocumentExpiryWorker(IConnectionMultiplexer redis, Microsoft.Extensions.Logging.ILogger<DocumentExpiryWorker> logger, IServiceScopeFactory serviceScopeFactory, IHostEnvironment env, ISystemClock clock) 
            : base(redis, logger, serviceScopeFactory, env, clock) { }

        public new Task ProcessAsync(CancellationToken ct) => base.ProcessAsync(ct);
    }

    [Fact]
    public async Task DocumentExpiryWorker_WithExpiringDoc_EnqueuesAlert()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var idGenerator = Substitute.For<IIdGenerator>();
        idGenerator.NewId<DocumentRecordId>().Returns(new DocumentRecordId(Guid.NewGuid()));

        var expiryDate = new DateOnly(2026, 10, 15); // 5 days from now

        var document = DocumentRecord.Create(
            studentId,
            DocumentType.MedicalExam,
            expiryDate,
            idGenerator,
            "s3key");

        _dbContext.DocumentRecords.Add(document);
        await _dbContext.SaveChangesAsync();

        // Act
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert
        var updatedDoc = await _dbContext.DocumentRecords.FirstAsync();
        updatedDoc.LastAlertSentAt.Should().Be(_clock.UtcNow);

        var outboxMessage = await _dbContext.OutboxMessages.FirstOrDefaultAsync();
        outboxMessage.Should().NotBeNull();
        outboxMessage!.Type.Should().Be(nameof(DocumentExpiryAlertRequestedEvent));
    }
}
