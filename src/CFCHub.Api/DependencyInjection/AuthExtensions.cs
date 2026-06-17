using System;
using System.Security.Cryptography;
using CFCHub.Application.Common.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CFCHub.Api.DependencyInjection;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolve the public key from Secrets Manager synchronously at startup
        var serviceProvider = services.BuildServiceProvider();
        var secretsManager = serviceProvider.GetRequiredService<ISecretsManagerService>();

        var publicKeyArn = configuration["Jwt:PublicKeyArn"]
            ?? Environment.GetEnvironmentVariable("CFCHUB_JWT_PUBLIC_KEY_ARN");

        RsaSecurityKey? rsaKey = null;

        if (!string.IsNullOrEmpty(publicKeyArn))
        {
            var publicKeyPem = secretsManager.GetSecretAsync(publicKeyArn).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(publicKeyPem))
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem.ToCharArray());
                rsaKey = new RsaSecurityKey(rsa);
            }
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaKey,
                    ValidAlgorithms = new[] { "RS256" },
                    ValidateIssuer = false, // Configured per tenant if needed
                    ValidateAudience = false
                };
            });

        return services;
    }
}
