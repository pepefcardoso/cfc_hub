using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Infrastructure.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CFCHub.Api.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IRateLimiter rateLimiter,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IHostEnvironment hostEnvironment)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        int limit = 120;
        TimeSpan window = TimeSpan.FromMinutes(1);

        if (path.StartsWith("/api/v1/auth/", StringComparison.OrdinalIgnoreCase) && method == HttpMethods.Post)
        {
            limit = 10;
            window = TimeSpan.FromMinutes(15);
        }
        else if (path.StartsWith("/api/v1/scheduling/", StringComparison.OrdinalIgnoreCase))
        {
            if (method == HttpMethods.Post || method == HttpMethods.Patch)
            {
                limit = 30;
                window = TimeSpan.FromMinutes(1);
            }
            else if (method == HttpMethods.Get)
            {
                limit = 120;
                window = TimeSpan.FromMinutes(1);
            }
        }
        else if (path.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase) && 
                 (path.Contains("/detran/", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/cnh-status", StringComparison.OrdinalIgnoreCase)) && 
                 method == HttpMethods.Get)
        {
            limit = 5;
            window = TimeSpan.FromMinutes(1);
        }
        else if (path.StartsWith("/api/v1/public/", StringComparison.OrdinalIgnoreCase) && method == HttpMethods.Get)
        {
            limit = 60;
            window = TimeSpan.FromMinutes(1);
        }

        var routeTemplate = context.GetEndpoint() is RouteEndpoint re ? re.RoutePattern.RawText : path;
        var endpointHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(routeTemplate ?? ""));
        var endpointHash = Convert.ToHexString(endpointHashBytes).ToLowerInvariant();

        var userId = currentUserService.UserId != Guid.Empty 
            ? currentUserService.UserId.ToString() 
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var tenant = tenantContext.IsResolved ? tenantContext.TenantSlug : "global";
        var env = hostEnvironment.EnvironmentName;

        var key = RedisKeys.RateLimit(env, tenant, endpointHash, userId);

        var (allowed, retryAfterSeconds) = await rateLimiter.CheckLimitAsync(key, limit, window, context.RequestAborted);

        if (!allowed)
        {
            _logger.LogWarning("Rate limit exceeded for key: {Key}. Retry after: {RetryAfter}s", key, retryAfterSeconds);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            
            // Generate a problem details response
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            var traceId = currentUserService.TraceId;
            if (string.IsNullOrEmpty(traceId)) traceId = context.TraceIdentifier;

            var problemDetails = $@"{{
  ""type"": ""https://tools.ietf.org/html/rfc7231#section-6.5.4"",
  ""title"": ""Too Many Requests"",
  ""status"": 429,
  ""detail"": ""Limite de requisições excedido. Tente novamente mais tarde."",
  ""traceId"": ""{traceId}""
}}";
            await context.Response.WriteAsync(problemDetails, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
