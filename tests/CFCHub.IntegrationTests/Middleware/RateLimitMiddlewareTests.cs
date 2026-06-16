using System;
using System.IO;
using System.Threading.Tasks;
using CFCHub.Api.Middleware;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace CFCHub.IntegrationTests.Middleware;

public class RateLimitMiddlewareTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IConnectionMultiplexer _redis = null!;
    private RedisRateLimiter _rateLimiter = null!;

    public RateLimitMiddlewareTests()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        _redis = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
        _rateLimiter = new RedisRateLimiter(_redis);
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task RateLimit_AuthEndpoint_Blocks11thRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/auth/login";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        var tenantContext = new MockTenantContext();
        var currentUserService = new MockCurrentUserService();
        var hostEnvironment = new MockHostEnvironment();

        var middleware = new RateLimitMiddleware(
            ctx => Task.CompletedTask,
            NullLogger<RateLimitMiddleware>.Instance);

        // Act - 10 allowed requests
        for (int i = 0; i < 10; i++)
        {
            var ctx = CreateClone(context);
            await middleware.InvokeAsync(ctx, _rateLimiter, tenantContext, currentUserService, hostEnvironment);
            ctx.Response.StatusCode.Should().NotBe(429);
        }

        // 11th request should be blocked
        var blockedCtx = CreateClone(context);
        await middleware.InvokeAsync(blockedCtx, _rateLimiter, tenantContext, currentUserService, hostEnvironment);

        // Assert
        blockedCtx.Response.StatusCode.Should().Be(429);
        blockedCtx.Response.Headers.Should().ContainKey("Retry-After");
        
        var retryAfterVal = int.Parse(blockedCtx.Response.Headers["Retry-After"]!);
        retryAfterVal.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(900); // 15 mins = 900s
        
        blockedCtx.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(blockedCtx.Response.Body).ReadToEndAsync();
        responseBody.Should().Contain("Too Many Requests");
    }

    private DefaultHttpContext CreateClone(DefaultHttpContext original)
    {
        var clone = new DefaultHttpContext();
        clone.Request.Method = original.Request.Method;
        clone.Request.Path = original.Request.Path;
        clone.Connection.RemoteIpAddress = original.Connection.RemoteIpAddress;
        clone.Response.Body = new MemoryStream();
        return clone;
    }

    private class MockTenantContext : ITenantContext
    {
        public bool IsResolved => true;
        public Guid TenantId => Guid.NewGuid();
        public string TenantSlug => "test-tenant";
        public string SchemaName => "cfc_test_tenant";
    }

    private class MockCurrentUserService : ICurrentUserService
    {
        public Guid UserId => Guid.Empty;
        public RoleType Role => RoleType.Receptionist;
        public string IpAddress => "127.0.0.1";
        public string? UserAgent => "test";
        public string TraceId => "trace-123";
    }

    private class MockHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "CFCHub.Api";
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
