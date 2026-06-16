using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Identity;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Identity;
using CFCHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Testcontainers.PostgreSql;
using Xunit;

namespace CFCHub.IntegrationTests.Middleware;

public class TenantResolutionMiddlewareTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private IServiceProvider _serviceProvider = null!;

    public TenantResolutionMiddlewareTests()
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
        services.AddScoped<ITenantContext, TenantContext>();
        
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(_dbContainer.GetConnectionString());
        });
        
        services.AddScoped<TenantMigrationOrchestrator>();
        services.AddScoped<ITenantRegistry, TenantRegistry>();
        services.AddScoped<ITenantCacheService, MockTenantCacheService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        _serviceProvider = services.BuildServiceProvider();
        
        // Run initial migrations
        using var scope = _serviceProvider.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<TenantMigrationOrchestrator>();
        await orchestrator.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task TenantResolution_WithValidJwt_PopulatesTenantContext()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        var privateKey = new RsaSecurityKey(rsa);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Insert tenant
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO public.tenants (id, slug, schema_name, status) VALUES ('{tenantId}', 'test_slug', 'cfc_test_slug', 'Active') ON CONFLICT DO NOTHING;";
        await command.ExecuteNonQueryAsync();

        // Create JWT
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, RoleType.Instructor.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {jwt}";

        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var tenantCacheService = scope.ServiceProvider.GetRequiredService<ITenantCacheService>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        var jwtValidationService = new JwtValidationService(new MockSecretsManager(publicKeyPem));

        var nextCalled = false;
        var middleware = new CFCHub.Api.Middleware.TenantResolutionMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; }, 
            NullLogger<CFCHub.Api.Middleware.TenantResolutionMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(httpContext, tenantContext, tenantCacheService, tenantRegistry, jwtValidationService, currentUserService, dbContext);

        // Assert
        nextCalled.Should().BeTrue();
        tenantContext.IsResolved.Should().BeTrue();
        tenantContext.TenantId.Should().Be(tenantId);
        currentUserService.UserId.Should().Be(userId);
    }

    private class MockSecretsManager : ISecretsManagerService
    {
        private readonly string _publicKeyPem;
        public MockSecretsManager(string publicKeyPem) => _publicKeyPem = publicKeyPem;
        public Task<string?> GetSecretAsync(string arn, System.Threading.CancellationToken ct = default) => Task.FromResult<string?>(_publicKeyPem);
    }

    private class MockTenantCacheService : ITenantCacheService
    {
        public Task<TenantCacheItem?> GetAsync(string slug, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult<TenantCacheItem?>(null);
        public Task<TenantCacheItem?> GetByIdAsync(Guid id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult<TenantCacheItem?>(null);
        public Task SetAsync(string slug, TenantCacheItem tenantContext, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetByIdAsync(Guid id, TenantCacheItem tenantContext, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
