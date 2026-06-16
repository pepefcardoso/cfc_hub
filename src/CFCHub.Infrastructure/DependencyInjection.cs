using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Scheduling;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CFCHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        
        services.AddScoped<TenantMigrationOrchestrator>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<TenantProvisioningService>(); // Keep this if tests use it directly
        services.AddScoped<ITenantRegistry, TenantRegistry>();
        
        services.AddSingleton<AuditInterceptor>();
        
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IOutboxService, CFCHub.Infrastructure.Outbox.OutboxService>();
        
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? Environment.GetEnvironmentVariable("CFCHUB_DB_CONNECTION_STRING");
            options.UseNpgsql(connectionString);
            
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
            options.AddInterceptors(auditInterceptor);
            options.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        });

        var redisConnectionString = configuration.GetConnectionString("Redis") 
            ?? Environment.GetEnvironmentVariable("CFCHUB_REDIS_CONNECTION_STRING") 
            ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnectionString));
            
        services.AddSingleton<ISchedulingLockService, RedisLockService>();
        
        services.AddScoped<IAvailabilityCacheService, AvailabilityCacheService>();
        services.AddScoped<IAvailabilityCalculatorService, CFCHub.Infrastructure.Scheduling.AvailabilityCalculatorService>();
        services.AddSingleton<ITenantCacheService, TenantCacheService>();
        
        services.AddSingleton<CFCHub.Application.Common.Security.ISecretsManagerService, CFCHub.Infrastructure.Security.SecretsManagerService>();
        services.AddSingleton<CFCHub.Application.Common.Security.IDataProtectionService, CFCHub.Infrastructure.Security.DataProtectionService>();
            
        return services;
    }
}
