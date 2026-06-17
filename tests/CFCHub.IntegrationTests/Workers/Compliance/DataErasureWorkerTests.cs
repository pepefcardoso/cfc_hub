using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
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

namespace CFCHub.IntegrationTests.Workers.Compliance;

public class DataErasureWorkerTests : IAsyncLifetime
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
    private TestDataErasureWorker _worker = null!;
    private Guid _tenantId = Guid.NewGuid();
    private string _tenantSlug = "test_cfc";
    private string _schemaName = "cfc_test_cfc";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero));

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
        services.AddSingleton<ILogger<DataErasureWorker>>(NullLogger<DataErasureWorker>.Instance);

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Test");

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();

#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_schemaName}\";");
#pragma warning restore EF1002
        await _dbContext.Database.EnsureCreatedAsync();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _worker = new TestDataErasureWorker(_redis, NullLogger<DataErasureWorker>.Instance, scopeFactory, env);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
        _redis.Dispose();
        await _redisContainer.DisposeAsync();
    }

    private class TestDataErasureWorker : DataErasureWorker
    {
        public TestDataErasureWorker(IConnectionMultiplexer redis, ILogger<DataErasureWorker> logger, IServiceScopeFactory serviceScopeFactory, IHostEnvironment env) 
            : base(redis, logger, serviceScopeFactory, env) { }

        public new Task ProcessAsync(CancellationToken ct) => base.ProcessAsync(ct);
    }

    [Fact]
    public async Task DataErasureWorker_AnonymizesStudent_RetainsPaidInvoices()
    {
        // Arrange
        var idGenerator = Substitute.For<IIdGenerator>();
        var studentId = new StudentId(Guid.NewGuid());
        var student = Student.Create(
            studentId, "John Doe", "12345678901", "RG123", "john@example.com", "5551234", 
            new DateOnly(1990, 1, 1), 
            new Address("St", "1", null, "District", "City", "ST", "12345-000"),
            _clock, idGenerator);

        student.RequestErasure();
        _dbContext.Students.Add(student);

        var enrollmentId = new EnrollmentId(Guid.NewGuid());
        var enrollment = CFCHub.Domain.Enrollment.Enrollment.Enroll(enrollmentId, studentId, CnhCategory.B, _clock);
        _dbContext.Enrollments.Add(enrollment);

        var oldPayment = Payment.Create(new PaymentId(Guid.NewGuid()), studentId, enrollmentId, new Money(100), PaymentMethod.CreditCard);
        oldPayment.CreatedAt = _clock.UtcNow.AddYears(-6);
        _dbContext.Payments.Add(oldPayment);

        var recentPayment = Payment.Create(new PaymentId(Guid.NewGuid()), studentId, enrollmentId, new Money(200), PaymentMethod.Pix);
        recentPayment.CreatedAt = _clock.UtcNow.AddYears(-2);
        _dbContext.Payments.Add(recentPayment);

        var erasureRequest = DataErasureRequest.Create(new DataErasureRequestId(Guid.NewGuid()), studentId, _clock);
        _dbContext.DataErasureRequests.Add(erasureRequest);

        await _dbContext.SaveChangesAsync();

        // Act
        await _worker.ProcessAsync(CancellationToken.None);

        // Assert
        var updatedStudent = await _dbContext.Students.FirstAsync();
        updatedStudent.Name.Should().Be("[REMOVIDO]");
        updatedStudent.Email.Should().Be("[REMOVIDO]");
        updatedStudent.Phone.Should().Be("[REMOVIDO]");
        updatedStudent.Rg.Should().BeNull();

        var updatedEnrollment = await _dbContext.Enrollments.IgnoreQueryFilters().FirstAsync();
        updatedEnrollment.DeletedAt.Should().NotBeNull();

        var payments = await _dbContext.Payments.ToListAsync();
        payments.Should().HaveCount(1);
        payments.First().Id.Should().Be(recentPayment.Id);

        var updatedRequest = await _dbContext.DataErasureRequests.FirstAsync();
        updatedRequest.Status.Should().Be(DataErasureRequestStatus.Completed);
        updatedRequest.CompletedAt.Should().Be(_clock.UtcNow);

        var outboxMessage = await _dbContext.OutboxMessages.FirstOrDefaultAsync(m => m.Type == "DataErasureCompleteNotified");
        outboxMessage.Should().NotBeNull();
    }
}
