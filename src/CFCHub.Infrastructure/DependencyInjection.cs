using CFCHub.Application.Common.Interfaces;
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
        
        services.AddSingleton<AuditInterceptor>();
        
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? Environment.GetEnvironmentVariable("CFCHUB_DB_CONNECTION_STRING");
            options.UseNpgsql(connectionString);
            
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
            options.AddInterceptors(auditInterceptor);
        });

        var redisConnectionString = configuration.GetConnectionString("Redis") 
            ?? Environment.GetEnvironmentVariable("CFCHUB_REDIS_CONNECTION_STRING") 
            ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnectionString));
            
        return services;
    }
}
