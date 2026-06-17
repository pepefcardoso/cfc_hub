using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Security;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace CFCHub.Infrastructure.Identity;

public interface IJwtValidationService
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken ct = default);
}

public class JwtValidationService : IJwtValidationService
{
    private readonly ISecretsManagerService _secretsManager;
    private readonly ILogger<JwtValidationService> _logger;
    private const string PublicKeyArn = "CFCHUB_JWT_PUBLIC_KEY_ARN";

    public JwtValidationService(ISecretsManagerService secretsManager, ILogger<JwtValidationService> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var publicKeyPem = await _secretsManager.GetSecretAsync(PublicKeyArn, ct);
        if (string.IsNullOrEmpty(publicKeyPem))
            return null;

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate JWT token");
            return null;
        }
    }
}
