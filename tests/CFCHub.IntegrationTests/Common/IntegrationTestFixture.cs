using System;
using System.Net.Http;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace CFCHub.IntegrationTests.Common;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public WebApplicationFactory<CFCHub.Api.Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        Environment.SetEnvironmentVariable("CFCHUB_DB_CONNECTION_STRING", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("CFCHUB_REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString());

        Factory = new WebApplicationFactory<CFCHub.Api.Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove the default ITenantContext registration
                services.RemoveAll<ITenantContext>();
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }

        await _dbContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}
