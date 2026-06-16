using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.ExternalServices.Detran;
using CFCHub.Infrastructure.ExternalServices.Detran.Adapters;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;
using Xunit;

namespace CFCHub.UnitTests.Infrastructure.ExternalServices.Detran;

public class DetranHttpClientTests
{
    private readonly IStateDetranAdapterFactory _adapterFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DetranHttpClient> _logger;
    private readonly IDetranAdapter _adapter;

    public DetranHttpClientTests()
    {
        _adapterFactory = Substitute.For<IStateDetranAdapterFactory>();
        _redis = Substitute.For<IConnectionMultiplexer>();
        _db = Substitute.For<IDatabase>();
        _tenantContext = Substitute.For<ITenantContext>();
        _configuration = Substitute.For<IConfiguration>();
        _logger = Substitute.For<ILogger<DetranHttpClient>>();
        _adapter = Substitute.For<IDetranAdapter>();

        _redis.GetDatabase().Returns(_db);
        _tenantContext.TenantSlug.Returns("test-tenant");
        _configuration["Detran:State"].Returns("SP");
        
        _adapterFactory.GetAdapter(BrazilianState.SP).Returns(_adapter);
    }

    private DetranHttpClient CreateSut()
    {
        return new DetranHttpClient(
            _adapterFactory,
            _redis,
            _tenantContext,
            _configuration,
            _logger);
    }

    [Fact]
    public async Task GetCnhStatusAsync_CacheHit_ReturnsWithoutCallingAdapter()
    {
        // Arrange
        var cpf = "12345678909";
        var cpfHash = RedisKeys.CpfHash(cpf);
        // We know ASPNETCORE_ENVIRONMENT might be null so it defaults to "Development", or we can mock env but Environment.GetEnvironmentVariable is static.
        // Let's rely on the actual key that gets generated during test.
        var expectedResult = new CnhStatusResult(true, "Ativa", new DateOnly(2030, 1, 1), 0);
        
        _db.StringGetAsync(Arg.Any<RedisKey>()).Returns(JsonSerializer.Serialize(expectedResult));

        var sut = CreateSut();

        // Act
        var result = await sut.GetCnhStatusAsync(cpf);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        await _adapter.DidNotReceiveWithAnyArgs().GetCnhStatusAsync(default!, default);
    }

    [Fact]
    public async Task GetCnhStatusAsync_CacheMiss_CallsAdapterAndCachesResult()
    {
        // Arrange
        var cpf = "12345678909";
        var expectedResult = new CnhStatusResult(true, "Ativa", new DateOnly(2030, 1, 1), 0);

        _db.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);
        _adapter.GetCnhStatusAsync(cpf, Arg.Any<CancellationToken>()).Returns(expectedResult);

        var sut = CreateSut();

        // Act
        var result = await sut.GetCnhStatusAsync(cpf);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        await _adapter.Received(1).GetCnhStatusAsync(cpf, Arg.Any<CancellationToken>());
        await _db.Received(1).StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), TimeSpan.FromSeconds(86400));
    }

    [Fact]
    public async Task GetCnhStatusAsync_AdapterThrowsException_ReturnsUnavailableWithoutThrowing()
    {
        // Arrange
        var cpf = "12345678909";

        _db.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);
        _adapter.GetCnhStatusAsync(cpf, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Network error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetCnhStatusAsync(cpf);

        // Assert
        result.Should().BeEquivalentTo(CnhStatusResult.Unavailable);
    }
}
