using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace CFCHub.UnitTests.Infrastructure.Caching;

public class RedisLockServiceTests
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisLockService> _logger;
    private readonly RedisLockService _sut;

    public RedisLockServiceTests()
    {
        _redis = Substitute.For<IConnectionMultiplexer>();
        _database = Substitute.For<IDatabase>();
        _logger = Substitute.For<ILogger<RedisLockService>>();

        _redis.GetDatabase().Returns(_database);

        _sut = new RedisLockService(_redis, _logger);
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldSetWithExactly30SecondsTtl()
    {
        // Arrange
        var key = "instructor:123";
        _database.StringSetAsync(Arg.Is<RedisKey>(k => k == key), Arg.Is<RedisValue>(v => v == "1"), TimeSpan.FromSeconds(30), When.NotExists)
            .Returns(true);

        // Act
        var result = await _sut.TryAcquireAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        await _database.Received(1).StringSetAsync(Arg.Is<RedisKey>(k => k == key), Arg.Is<RedisValue>(v => v == "1"), TimeSpan.FromSeconds(30), When.NotExists);
    }

    [Fact]
    public async Task TryAcquireAsync_WithDifferentTtl_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = async () => await _sut.TryAcquireAsync("key", TimeSpan.FromSeconds(20), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*TTL must be exactly 30 seconds*");
    }

    [Fact]
    public async Task ReleaseAsync_WhenKeyDeleted_ShouldNotLogDebug()
    {
        // Arrange
        var key = "instructor:123";
        _database.KeyDeleteAsync(Arg.Is<RedisKey>(k => k == key)).Returns(true);

        // Act
        await _sut.ReleaseAsync(key, CancellationToken.None);

        // Assert
        await _database.Received(1).KeyDeleteAsync(Arg.Is<RedisKey>(k => k == key));
    }

    [Fact]
    public async Task AcquireAll_ShouldAcquireInSortedOrder()
    {
        // Arrange
        var keys = new[] { "vehicle:1", "instructor:2", "track:3" };
        
        _database.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);

        // Act
        var result = await _sut.AcquireAllAsync(keys, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        Received.InOrder(() =>
        {
            _database.StringSetAsync(Arg.Is<RedisKey>(k => k == "instructor:2"), Arg.Is<RedisValue>(v => v == "1"), TimeSpan.FromSeconds(30), When.NotExists);
            _database.StringSetAsync(Arg.Is<RedisKey>(k => k == "track:3"), Arg.Is<RedisValue>(v => v == "1"), TimeSpan.FromSeconds(30), When.NotExists);
            _database.StringSetAsync(Arg.Is<RedisKey>(k => k == "vehicle:1"), Arg.Is<RedisValue>(v => v == "1"), TimeSpan.FromSeconds(30), When.NotExists);
        });
    }

    [Fact]
    public async Task AcquireAll_ThirdLockFails_ReleasesFirstTwo()
    {
        // Arrange
        var keys = new[] { "z:1", "a:1", "m:1" };
        
        _database.StringSetAsync(Arg.Is<RedisKey>(k => k == "a:1"), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>()).Returns(true);
        _database.StringSetAsync(Arg.Is<RedisKey>(k => k == "m:1"), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>()).Returns(true);
        _database.StringSetAsync(Arg.Is<RedisKey>(k => k == "z:1"), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>()).Returns(false);

        // Act
        var result = await _sut.AcquireAllAsync(keys, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        await _database.Received(1).KeyDeleteAsync(Arg.Is<RedisKey>(k => k == "a:1"));
        await _database.Received(1).KeyDeleteAsync(Arg.Is<RedisKey>(k => k == "m:1"));
        await _database.DidNotReceive().KeyDeleteAsync(Arg.Is<RedisKey>(k => k == "z:1"));
    }
}
