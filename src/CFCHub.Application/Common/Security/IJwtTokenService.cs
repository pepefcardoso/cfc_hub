using System;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;

namespace CFCHub.Application.Common.Security;

public record JwtTokenResult(
    string AccessToken,
    string Jti,
    DateTimeOffset ExpiresAt
);

public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the given user.
    /// Implementation MUST use RS256 algorithm (never HS256).
    /// Implementation MUST include claims: sub, jti, tenant_id, role, iat, exp (1h).
    /// </summary>
    JwtTokenResult GenerateToken(StaffUser user, ITenantContext tenantContext);
}
