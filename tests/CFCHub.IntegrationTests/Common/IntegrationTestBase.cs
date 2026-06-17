using System;
using System.Net.Http;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace CFCHub.IntegrationTests.Common;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    protected readonly IntegrationTestFixture _fixture;
    
    protected HttpClient Client = null!;
    protected string TenantSchema { get; } = $"cfc_test_{Guid.NewGuid():N}";
    protected Guid TenantId { get; } = Guid.NewGuid();

    protected IServiceScope Scope = null!;
    protected ISender Sender = null!;
    protected AppDbContext DbContext = null!;
    protected IServiceProvider ServiceProvider = null!;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        // For each test, we create a specialized factory with our specific schema context
        var customizedFactory = _fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<ITenantContext>(_ => new TestTenantContext(TenantId, TenantSchema));
            });
        });

        Client = customizedFactory.CreateClient();
        Scope = customizedFactory.Services.CreateScope();
        ServiceProvider = Scope.ServiceProvider;
        Sender = ServiceProvider.GetRequiredService<ISender>();
        DbContext = ServiceProvider.GetRequiredService<AppDbContext>();

        // Create schema and run migrations for this test instance
#pragma warning disable EF1003
        await DbContext.Database.ExecuteSqlRawAsync("CREATE SCHEMA " + TenantSchema);
#pragma warning restore EF1003
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
#pragma warning disable EF1003
            await DbContext.Database.ExecuteSqlRawAsync("DROP SCHEMA " + TenantSchema + " CASCADE");
#pragma warning restore EF1003
        }

        Scope?.Dispose();
        Client?.Dispose();
    }

    private class TestTenantContext : ITenantContext
    {
        public TestTenantContext(Guid tenantId, string schemaName)
        {
            TenantId = tenantId;
            SchemaName = schemaName;
            TenantSlug = schemaName.Replace("cfc_", "");
        }

        public Guid TenantId { get; }
        public string TenantSlug { get; }
        public string SchemaName { get; }
        public bool IsResolved => true;
    }
}
