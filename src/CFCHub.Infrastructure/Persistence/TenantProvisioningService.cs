using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CFCHub.Infrastructure.Persistence;

public class TenantProvisioningService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(IServiceProvider serviceProvider, ILogger<TenantProvisioningService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProvisionAsync(string slug, Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("Provisioning new tenant {Slug} ({Id})", slug, tenantId);
        var schemaName = $"cfc_{slug}";

        using var scope = _serviceProvider.CreateScope();
        
        var templateContext = CreateContextForSchema(scope.ServiceProvider, "__template");
        var connection = templateContext.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        
        if (wasClosed)
        {
            await connection.OpenAsync(ct);
        }

        using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = $"CREATE SCHEMA IF NOT EXISTS {schemaName};";
            await command.ExecuteNonQueryAsync(ct);

            var tenantContext = CreateContextForSchema(scope.ServiceProvider, schemaName);
            
            try 
            {
                await tenantContext.Database.MigrateAsync(ct);

                command.CommandText = @"
                    INSERT INTO public.tenants (id, slug, schema_name, status)
                    VALUES (@id, @slug, @schemaName, 'Active');";
                
                var paramId = command.CreateParameter();
                paramId.ParameterName = "@id";
                paramId.Value = tenantId;
                command.Parameters.Add(paramId);

                var paramSlug = command.CreateParameter();
                paramSlug.ParameterName = "@slug";
                paramSlug.Value = slug;
                command.Parameters.Add(paramSlug);

                var paramSchema = command.CreateParameter();
                paramSchema.ParameterName = "@schemaName";
                paramSchema.Value = schemaName;
                command.Parameters.Add(paramSchema);

                await command.ExecuteNonQueryAsync(ct);
                
                await transaction.CommitAsync(ct);
                _logger.LogInformation("Tenant {Slug} provisioned successfully.", slug);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to provision tenant {Slug}. Rolling back schema and transaction.", slug);
                await transaction.RollbackAsync(ct);
                
                using var dropCommand = connection.CreateCommand();
                dropCommand.CommandText = $"DROP SCHEMA IF EXISTS {schemaName} CASCADE;";
                await dropCommand.ExecuteNonQueryAsync(CancellationToken.None);
                
                throw;
            }
        }
        finally
        {
            if (wasClosed && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private AppDbContext CreateContextForSchema(IServiceProvider sp, string schemaName)
    {
        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var clock = sp.GetRequiredService<ISystemClock>();
        
        var tenantContext = new ProvisioningTenantContext(schemaName);
        return new AppDbContext(options, tenantContext, clock);
    }

    private class ProvisioningTenantContext : ITenantContext
    {
        public ProvisioningTenantContext(string schemaName)
        {
            SchemaName = schemaName;
        }

        public Guid TenantId => Guid.Empty;
        public string TenantSlug => SchemaName.Replace("cfc_", "");
        public string SchemaName { get; }
        public bool IsResolved => true;
    }
}
