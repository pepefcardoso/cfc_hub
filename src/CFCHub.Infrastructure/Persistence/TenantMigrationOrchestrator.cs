using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CFCHub.Infrastructure.Persistence;

public class TenantMigrationOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantMigrationOrchestrator> _logger;

    public TenantMigrationOrchestrator(IServiceProvider serviceProvider, ILogger<TenantMigrationOrchestrator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting TenantMigrationOrchestrator...");

        using var scope = _serviceProvider.CreateScope();
        
        var templateContext = CreateContextForSchema(scope.ServiceProvider, "__template");
        
        var pendingTemplateMigrations = await templateContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingTemplateMigrations.Any())
        {
            _logger.LogInformation("Applying {Count} pending migrations to __template schema.", pendingTemplateMigrations.Count());
            await templateContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("No pending migrations for __template schema.");
        }

        var activeTenants = await GetActiveTenantSlugsAsync(templateContext, cancellationToken);

        foreach (var slug in activeTenants)
        {
            var schemaName = $"cfc_{slug}";
            var tenantContext = CreateContextForSchema(scope.ServiceProvider, schemaName);
            
            var pendingTenantMigrations = await tenantContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingTenantMigrations.Any())
            {
                foreach (var migration in pendingTenantMigrations)
                {
                    _logger.LogInformation("Applying migration {Migration} to tenant schema {Schema}", migration, schemaName);
                }
                
                await tenantContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Successfully migrated tenant schema {Schema}", schemaName);
            }
        }
        
        _logger.LogInformation("TenantMigrationOrchestrator completed successfully.");
    }

    private AppDbContext CreateContextForSchema(IServiceProvider sp, string schemaName)
    {
        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var clock = sp.GetRequiredService<ISystemClock>();
        
        var tenantContext = new OrchestratorTenantContext(schemaName);
        return new AppDbContext(options, tenantContext, clock);
    }

    private async Task<List<string>> GetActiveTenantSlugsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var slugs = new List<string>();
        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        
        try
        {
            if (wasClosed)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            
            command.CommandText = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' AND table_name = 'tenants'
                );";
            
            var tableExists = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
            if (!tableExists)
            {
                command.CommandText = @"
                    CREATE TABLE public.tenants (
                        id UUID PRIMARY KEY,
                        slug TEXT NOT NULL UNIQUE,
                        schema_name TEXT NOT NULL,
                        status TEXT NOT NULL
                    );";
                await command.ExecuteNonQueryAsync(cancellationToken);
                return slugs;
            }

            command.CommandText = "SELECT slug FROM public.tenants WHERE status = 'Active';";
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                slugs.Add(reader.GetString(0));
            }
        }
        finally
        {
            if (wasClosed && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        return slugs;
    }

    private class OrchestratorTenantContext : ITenantContext
    {
        public OrchestratorTenantContext(string schemaName)
        {
            SchemaName = schemaName;
        }

        public Guid TenantId => Guid.Empty;
        public string TenantSlug => SchemaName.Replace("cfc_", "");
        public string SchemaName { get; }
        public bool IsResolved => true;
    }
}
