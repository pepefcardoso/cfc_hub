using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Xunit;

namespace CFCHub.IntegrationTests.Infrastructure;

public class TenantProvisioningServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private IServiceProvider _serviceProvider = null!;

    public TenantProvisioningServiceTests()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var services = new ServiceCollection();
        
        services.AddLogging();
        services.AddSingleton<ISystemClock, TestSystemClock>();
        services.AddScoped<ITenantContext, TestTenantContext>();
        
        // Register AppDbContext with Npgsql and replacing ModelCacheKeyFactory to mimic DI
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(_dbContainer.GetConnectionString());
            options.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        });
        
        // Register the services we are testing
        services.AddScoped<TenantMigrationOrchestrator>();
        services.AddScoped<TenantProvisioningService>();

        _serviceProvider = services.BuildServiceProvider();
        
        // We must run the initial migrations for __template first
        using var scope = _serviceProvider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<TenantMigrationOrchestrator>();
        await orchestrator.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task ProvisionAsync_ShouldCreateSchemaAndTables()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
        var tenantId = Guid.NewGuid();
        var slug = "test_school_abc";
        var expectedSchemaName = $"cfc_{slug}";

        // Act
        await provisioningService.ProvisionAsync(slug, tenantId, CancellationToken.None);

        // Assert
        // 1. Verify schema was created and context works
        var testContext = new TestTenantContext { SchemaName = expectedSchemaName };
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
        
        await using var dbContext = new AppDbContext(options, testContext, clock);
        
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        
        // Verify we can query an entity through EF
        var canQuery = await dbContext.Students.AnyAsync();
        canQuery.Should().BeFalse("table should be empty but accessible");

        // 2. Verify tenant was inserted in public.tenants
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM public.tenants WHERE slug = '{slug}' AND status = 'Active';";
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);
        count.Should().Be(1);
    }
    
    [Fact]
    public async Task InitializeAsync_ShouldApplyPendingMigrationsToActiveTenants()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<TenantMigrationOrchestrator>();
        
        var slug = "another_school";
        await provisioningService.ProvisionAsync(slug, Guid.NewGuid(), CancellationToken.None);
        
        // Act - running orchestrator again to simulate startup
        await orchestrator.InitializeAsync();
        
        // Assert
        var testContext = new TestTenantContext { SchemaName = $"cfc_{slug}" };
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();
        
        await using var dbContext = new AppDbContext(options, testContext, clock);
        var studentsExist = await dbContext.Students.AnyAsync();
        studentsExist.Should().BeFalse();
    }

    private class TestSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private class TestTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; } = Guid.NewGuid();
        public string TenantSlug { get; set; } = "test";
        public string SchemaName { get; set; } = "__template";
        public bool IsResolved => true;
    }
}
