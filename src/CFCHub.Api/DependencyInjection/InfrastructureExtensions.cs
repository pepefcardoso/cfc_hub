using System;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SimpleEmail;
using CFCHub.Application.Common.Email;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Email;
using CFCHub.Infrastructure.Identity;
using CFCHub.Infrastructure.Outbox;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Infrastructure.Persistence.Interceptors;
using CFCHub.Infrastructure.Persistence.Repositories;
using CFCHub.Infrastructure.Scheduling;
using CFCHub.Infrastructure.Security;
using CFCHub.Infrastructure.Services;
using CFCHub.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CFCHub.Api.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantContext, TenantContext>();

        services.AddScoped<TenantMigrationOrchestrator>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<ITenantRegistry, TenantRegistry>();

        services.AddSingleton<AuditInterceptor>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IOutboxService, OutboxService>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("CFCHUB_DB_CONNECTION_STRING");
            options.UseNpgsql(connectionString);

            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
            options.AddInterceptors(auditInterceptor);
            options.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        });

        // Repositories
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IDataErasureRequestRepository, DataErasureRequestRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IInstallmentRepository, InstallmentRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISchedulingRepository, SchedulingRepository>();
        services.AddScoped<IStaffUserRepository, StaffUserRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();

        // Redis
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? Environment.GetEnvironmentVariable("CFCHUB_REDIS_CONNECTION_STRING")
            ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<ISchedulingLockService, RedisLockService>();
        services.AddScoped<IAvailabilityCacheService, AvailabilityCacheService>();
        services.AddScoped<IAvailabilityCalculatorService, AvailabilityCalculatorService>();
        services.AddSingleton<ITenantCacheService, TenantCacheService>();

        // AWS and External Services
        services.AddAWSService<IAmazonSecretsManager>();
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonSimpleEmailService>();

        services.AddSingleton<ISecretsManagerService, SecretsManagerService>();
        services.AddSingleton<IDataProtectionService, DataProtectionService>();
        services.AddSingleton<IFileStorageService, S3FileStorageService>();
        services.AddSingleton<IEmailService, SesEmailService>();

        // Common Services
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();

        // Auth / Identity
        services.AddScoped<IJwtValidationService, JwtValidationService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddSingleton<IRateLimiter, RedisRateLimiter>();

        services.AddHealthChecks()
            .AddCheck<CFCHub.Infrastructure.Health.PostgreSqlHealthCheck>("postgres", tags: new[] { "ready" })
            .AddCheck<CFCHub.Infrastructure.Health.RedisHealthCheck>("redis", tags: new[] { "ready" })
            .AddCheck<CFCHub.Infrastructure.Health.S3HealthCheck>("s3", tags: new[] { "ready" });

        return services;
    }
}
