using System;
using System.Linq;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Auditing;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Infrastructure.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Xunit;

namespace CFCHub.IntegrationTests.Interceptors;

public class AuditInterceptorTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    private AppDbContext _dbContext = null!;
    private ICurrentUserService _currentUserService = null!;
    private ITenantContext _tenantContext = null!;
    private ISystemClock _clock = null!;
    private IConnectionMultiplexer _redis = null!;
    private CFCHub.Infrastructure.Caching.IAvailabilityCacheService _availabilityCache = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _tenantContext = Substitute.For<ITenantContext>();
        _tenantContext.SchemaName.Returns("cfc_test");
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tenantContext.TenantSlug.Returns("test");
        _tenantContext.IsResolved.Returns(true);

        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(RoleType.Receptionist);
        _currentUserService.IpAddress.Returns("127.0.0.1");
        _currentUserService.UserAgent.Returns("TestRunner");
        _currentUserService.TraceId.Returns("trace-123");

        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _redis = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        _availabilityCache = Substitute.For<CFCHub.Infrastructure.Caching.IAvailabilityCacheService>();

        var interceptor = new AuditInterceptor(_tenantContext, _currentUserService, _clock, _redis, _availabilityCache);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .AddInterceptors(interceptor);

        _dbContext = new AppDbContext(optionsBuilder.Options, _tenantContext, _clock);

#pragma warning disable EF1002
        await _dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{_tenantContext.SchemaName}\";");
#pragma warning restore EF1002
        
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task AuditInterceptor_OnStudentUpdate_WritesAuditLog()
    {
        // Arrange
        var idGenerator = Substitute.For<IIdGenerator>();
        var studentId = new StudentId(Guid.NewGuid());
        idGenerator.NewId<StudentId>().Returns(studentId);
        var student = Student.Create(
            studentId,
            "John Doe",
            "12345678901",
            "RG123",
            "john@example.com",
            "555-1234",
            new DateOnly(1990, 1, 1),
            new Address("Street", "123", "Apt 4", "Downtown", "City", "ST", "12345-678"),
            _clock,
            idGenerator);

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync();

        // Clear tracker so update is tracked cleanly
        _dbContext.ChangeTracker.Clear();

        var savedStudent = await _dbContext.Students.FirstAsync(s => s.Id == studentId);
        
        // Act - Update (Simulate requesting erasure)
        savedStudent.RequestErasure();
        await _dbContext.SaveChangesAsync();

        // Assert
        var logs = await _dbContext.AuditLogs.Where(x => x.EntityId == studentId.Value.ToString()).ToListAsync();
        
        logs.Should().HaveCount(2); // 1 for Create, 1 for Update

        var updateLog = logs.First(x => x.Action == "Modified");
        updateLog.EntityType.Should().Be(nameof(Student));
        updateLog.ActorUserId.Should().Be(_currentUserService.UserId);
        updateLog.ActorRole.Should().Be("Receptionist");
        updateLog.IpAddress.Should().Be("127.0.0.1");

        // Verify PII is NOT in plaintext in changed_fields for any log
        foreach(var log in logs)
        {
            log.ChangedFields.Should().NotContain("12345678901");
            log.ChangedFields.Should().NotContain("john@example.com");
            log.ChangedFields.Should().Contain("[encrypted]");
        }
    }
}
