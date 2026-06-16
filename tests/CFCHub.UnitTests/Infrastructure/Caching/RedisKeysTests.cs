using System;
using CFCHub.Infrastructure.Caching;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Infrastructure.Caching;

public class RedisKeysTests
{
    [Fact]
    public void SchedulingLockInstructor_ShouldReturnCorrectFormat()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = RedisKeys.SchedulingLockInstructor("dev", "tenant1", id);
        result.Should().Be("dev:tenant1:sched:lock:instructor:11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void SchedulingLockVehicle_ShouldReturnCorrectFormat()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var result = RedisKeys.SchedulingLockVehicle("prod", "tenant2", id);
        result.Should().Be("prod:tenant2:sched:lock:vehicle:22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public void SchedulingLockTrack_ShouldReturnCorrectFormat()
    {
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var result = RedisKeys.SchedulingLockTrack("stg", "tenant3", id);
        result.Should().Be("stg:tenant3:sched:lock:track:33333333-3333-3333-3333-333333333333");
    }

    [Fact]
    public void InstructorAvailability_ShouldReturnCorrectFormat()
    {
        var id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var date = new DateOnly(2023, 10, 25);
        var result = RedisKeys.InstructorAvailability("dev", "tenant4", id, date);
        result.Should().Be("dev:tenant4:sched:avail:instructor:44444444-4444-4444-4444-444444444444:2023-10-25");
    }

    [Fact]
    public void DetranCnhStatus_ShouldReturnCorrectFormat()
    {
        var result = RedisKeys.DetranCnhStatus("dev", "tenant5", "abc123hash");
        result.Should().Be("dev:tenant5:detran:cnh:abc123hash");
    }

    [Fact]
    public void RateLimit_ShouldReturnCorrectFormat()
    {
        var result = RedisKeys.RateLimit("prod", "tenant6", "hashxyz", "user789");
        result.Should().Be("prod:tenant6:rl:hashxyz:user789");
    }

    [Fact]
    public void StaffSession_ShouldReturnCorrectFormat()
    {
        var result = RedisKeys.StaffSession("dev", "tenant7", "jti-123");
        result.Should().Be("dev:tenant7:session:jti-123");
    }

    [Fact]
    public void OutboxWorkerLease_ShouldReturnCorrectFormat()
    {
        var result = RedisKeys.OutboxWorkerLease("prod", "tenant8");
        result.Should().Be("prod:tenant8:outbox:lease");
    }

    [Fact]
    public void TenantResolution_ShouldReturnCorrectFormat()
    {
        var result = RedisKeys.TenantResolution("dev", "slug9");
        result.Should().Be("dev:global:tenant:slug9");
    }

    [Fact]
    public void CpfHash_ShouldReturnCorrectLowerCaseHexString()
    {
        // Act
        var hash = RedisKeys.CpfHash("12345678909");

        // Assert
        hash.Should().Be("7ec94663084bd506d4f0c3e21042df233681fd7426e93f397c921b1d3e397bba");
        hash.Length.Should().Be(64);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CpfHash_WithInvalidInput_ShouldThrowArgumentException(string? invalidCpf)
    {
        var action = () => RedisKeys.CpfHash(invalidCpf!);
        action.Should().Throw<ArgumentException>();
    }
}
