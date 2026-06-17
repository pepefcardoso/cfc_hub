using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Identity;
using CFCHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CFCHub.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantCacheService tenantCacheService,
        ITenantRegistry tenantRegistry,
        IJwtValidationService jwtValidationService,
        ICurrentUserService currentUserService,
        AppDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 1. Skip resolution for unauthenticated routes
        if (IsUnauthenticatedRoute(path))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context); // Let authentication middleware handle 401
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // 2. Validate JWT signature
        var principal = await jwtValidationService.ValidateTokenAsync(token);
        if (principal == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // 3. Extract tenant_id claim
        var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            // Try to extract slug just in case the claim name is tenant_id but contains the slug, or if we use slug.
            var slug = tenantIdClaim;
            if (!string.IsNullOrEmpty(slug))
            {
                var cachedBySlug = await tenantCacheService.GetAsync(slug);
                if (cachedBySlug != null)
                {
                    tenantId = cachedBySlug.TenantId;
                }
                else
                {
                    var record = await tenantRegistry.GetBySlugAsync(slug);
                    if (record == null) throw new TenantNotFoundException(slug);
                    tenantId = record.Id;
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        // 4. Check Redis cache
        var cacheItem = await tenantCacheService.GetByIdAsync(tenantId);

        if (cacheItem == null)
        {
            // 5. On cache miss: query public.tenants
            var record = await tenantRegistry.GetByIdAsync(tenantId);

            // 6. If not found -> TenantNotFoundException; if status != Active -> ForbiddenException
            if (record == null)
            {
                throw new TenantNotFoundException(tenantId.ToString());
            }

            if (record.Status != "Active")
            {
                throw new ForbiddenException("TENANT_SUSPENDED");
            }

            cacheItem = new TenantCacheItem(record.SchemaName, record.Slug, record.Id);

            // 7. Cache result
            await tenantCacheService.SetByIdAsync(tenantId, cacheItem);
        }
        else
        {
            // We should still verify if tenant status changed, but the instruction doesn't mention checking status on cache hit.
            // It relies on TTL (300s).
        }

        // Call ITenantContext.Resolve
        ((TenantContext)tenantContext).Resolve(cacheItem.SchemaName, cacheItem.TenantSlug, cacheItem.TenantId);

        // 8. Set AppDbContext search_path
#pragma warning disable EF1002, EF1003
        await dbContext.Database.ExecuteSqlRawAsync($"SET search_path TO {cacheItem.SchemaName}, public;");
#pragma warning restore EF1002, EF1003

        // 9. Populate ICurrentUserService from JWT claims
        if (currentUserService is CurrentUserService svc)
        {
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                svc.UserId = userId;
            }

            var roleClaim = principal.FindFirst(ClaimTypes.Role) ?? principal.FindFirst("role");
            if (roleClaim != null && Enum.TryParse<RoleType>(roleClaim.Value, out var roleType))
            {
                svc.Role = roleType;
            }

            svc.IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            svc.UserAgent = context.Request.Headers["User-Agent"].ToString();
            svc.TraceId = context.TraceIdentifier;
        }

        await _next(context);
    }

    private bool IsUnauthenticatedRoute(string path)
    {
        return path.StartsWith("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/v1/public/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/webhooks", StringComparison.OrdinalIgnoreCase);
    }
}
