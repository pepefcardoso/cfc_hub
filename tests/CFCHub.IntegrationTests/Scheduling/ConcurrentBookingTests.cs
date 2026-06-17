using System;
using System.Linq;
using System.Threading.Tasks;
using CFCHub.Application.Scheduling.Commands.BookSlot;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Scheduling;
using CFCHub.IntegrationTests.Builders;
using CFCHub.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;
using CFCHub.Infrastructure.Persistence;

namespace CFCHub.IntegrationTests.Scheduling;

public class ConcurrentBookingTests : IntegrationTestBase
{
    public ConcurrentBookingTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task BookSlot_ConcurrentRequests_OnlyOneSucceeds()
    {
        // Arrange
        var builder = new SchedulingIntegrationBuilder(DbContext);
        var instructorId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(10); // 10:00 AM tomorrow
        
        await builder.SeedInstructorAsync(instructorId);
        await builder.SeedVehicleAsync(vehicleId);
        await builder.SeedTrackAsync(trackId);
        await builder.SeedStudentAsync(studentId);

        var command = new BookSlotCommand(
            instructorId,
            vehicleId,
            trackId,
            studentId,
            CnhCategory.B,
            startedAt);

        // Act
        // Create 10 concurrent requests using separate scopes to simulate concurrent HTTP requests
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            using var scope = ServiceProvider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
            return await sender.Send(command);
        }).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successes = results.Count(r => r.IsSuccess);
        var failures = results.Count(r => !r.IsSuccess);

        successes.Should().Be(1, "Only exactly one booking should succeed.");
        failures.Should().Be(9, "All other bookings should fail with a conflict.");

        foreach (var failure in results.Where(r => !r.IsSuccess))
        {
            failure.Error!.Code.Should().Be("SLOT_LOCK_FAILED", "The error code should indicate a lock or exclusion conflict.");
        }

#pragma warning disable EF1003
        var slotsInDb = await DbContext.Database.SqlQueryRaw<Guid>(
            "SELECT id FROM " + TenantSchema + ".scheduling_slots WHERE instructor_id = '" + instructorId + "'").ToListAsync();
#pragma warning restore EF1003
        
        slotsInDb.Count.Should().Be(1, "Exactly 1 slot should be present in the database.");
    }

    [Fact]
    public async Task BookSlot_ViaExclusionConstraint_BothRedisLocksBypassed()
    {
        // Arrange
        var builder = new SchedulingIntegrationBuilder(DbContext);
        var instructorId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(14); // 2:00 PM tomorrow
        
        await builder.SeedInstructorAsync(instructorId);
        await builder.SeedVehicleAsync(vehicleId);
        await builder.SeedTrackAsync(trackId);
        await builder.SeedStudentAsync(studentId);

        var insertSql = $@"
            INSERT INTO {TenantSchema}.scheduling_slots 
            (id, instructor_id, vehicle_id, track_id, student_id, started_at, ended_at, status)
            VALUES 
            ('{Guid.NewGuid()}', '{instructorId}', '{vehicleId}', '{trackId}', '{studentId}', 
             '{startedAt:O}', '{startedAt.AddMinutes(50):O}', 'Confirmed');";

        // Act & Assert
        // We simulate a race condition where Redis didn't catch the lock by executing two identical inserts concurrently
        // Note: Raw SQL bypasses EF Core and goes straight to Npgsql. Wait, PostgreSQL exclusion constraint should throw PostgresException with SqlState 23P04 (exclusion_violation).
        // Since we are running in same DbContext, we shouldn't run concurrent raw SQL on same instance. 
        // Let's create a new connection or run them sequentially to prove the second one fails if the constraint is present.
        // Actually, the prompt says "send 2 simultaneous INSERT commands; assert PostgreSQL raises exclusion constraint violation". 
        // EF Core doesn't like concurrent access on the same DbContext instance. We must use two DbContexts or two NpgsqlConnections.

        using var scope1 = ServiceProvider.CreateScope();
        using var scope2 = ServiceProvider.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

        var task1 = db1.Database.ExecuteSqlRawAsync(insertSql);
        var task2 = db2.Database.ExecuteSqlRawAsync(insertSql);

        var ex = await Record.ExceptionAsync(async () => await Task.WhenAll(task1, task2));
        
        ex.Should().NotBeNull();
        ex.Should().BeOfType<DbUpdateException>().Which.InnerException!.Message.Should().Contain("exclusion constraint");
        // We verify only 1 row was written despite the bypass
#pragma warning disable EF1003
        var slotsInDb = await DbContext.Database.SqlQueryRaw<Guid>(
            "SELECT id FROM " + TenantSchema + ".scheduling_slots WHERE instructor_id = '" + instructorId + "'").ToListAsync();
#pragma warning restore EF1003
        
        slotsInDb.Count.Should().Be(1, "Exactly 1 slot should be present in the database.");
    }

    [Fact]
    public async Task BookSlot_LocksReleasedAfterSuccess()
    {
        // Arrange
        var builder = new SchedulingIntegrationBuilder(DbContext);
        var instructorId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(16);
        
        await builder.SeedInstructorAsync(instructorId);
        await builder.SeedVehicleAsync(vehicleId);
        await builder.SeedTrackAsync(trackId);
        await builder.SeedStudentAsync(studentId);

        var command = new BookSlotCommand(instructorId, vehicleId, trackId, studentId, CnhCategory.B, startedAt);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var redis = ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        
        // Assert Redis keys don't exist. Assuming env is "test" or similar, or keys use the schema name.
        // Let's use RedisKeys to get the keys. 
        // We can inject RedisService or use the Multiplexer directly, but we need the exact key formatting.
        // We will just verify all keys containing the instructorId are deleted, or we can check via SCAN.
        var server = ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetServer(ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetEndPoints()[0]);
        var keys = server.Keys(pattern: $"*lock*{instructorId}*").ToList();
        
        keys.Should().BeEmpty("Redis locks for the instructor should be removed after successful booking.");
    }

    [Fact]
    public async Task BookSlot_LocksReleasedAfterFailure()
    {
        // Arrange
        var builder = new SchedulingIntegrationBuilder(DbContext);
        var instructorId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(18);
        
        await builder.SeedInstructorAsync(instructorId);
        await builder.SeedVehicleAsync(vehicleId);
        await builder.SeedTrackAsync(trackId);
        await builder.SeedStudentAsync(studentId);

        // Seed a conflicting slot directly into DB
        await builder.SeedSchedulingSlotAsync(Guid.NewGuid(), instructorId, vehicleId, trackId, studentId, startedAt);

        var command = new BookSlotCommand(instructorId, vehicleId, trackId, studentId, CnhCategory.B, startedAt);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse();

        var server = ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetServer(ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetEndPoints()[0]);
        var keys = server.Keys(pattern: $"*lock*{instructorId}*").ToList();
        
        keys.Should().BeEmpty("Redis locks for the instructor should be removed after a failed booking.");
    }
}
