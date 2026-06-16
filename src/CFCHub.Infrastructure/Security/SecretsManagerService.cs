using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using CFCHub.Application.Common.Security;
using Microsoft.Extensions.Logging;

namespace CFCHub.Infrastructure.Security;

public class SecretsManagerService : ISecretsManagerService
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly ILogger<SecretsManagerService> _logger;
    private readonly ConcurrentDictionary<string, (string value, DateTimeOffset cachedAt)> _cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public SecretsManagerService(IAmazonSecretsManager secretsManager, ILogger<SecretsManagerService> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<string?> GetSecretAsync(string arn, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(arn))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(arn, out var cached))
        {
            if (now - cached.cachedAt < CacheTtl)
            {
                return cached.value;
            }
        }

        if (arn.StartsWith("arn:aws:secretsmanager:us-east-1:000000000000"))
        {
            var envVar = Environment.GetEnvironmentVariable(arn);
            if (envVar != null)
            {
                _cache[arn] = (envVar, now);
                return envVar;
            }
        }

        string maskedArn = arn.Length > 20 ? arn.Substring(0, 20) + "..." : arn;

        try
        {
            var request = new GetSecretValueRequest
            {
                SecretId = arn
            };

            var response = await _secretsManager.GetSecretValueAsync(request, ct);

            if (response.SecretString != null)
            {
                _cache[arn] = (response.SecretString, now);
                return response.SecretString;
            }

            return null;
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogWarning("Secret not found. ARN: {MaskedArn}", maskedArn);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret from AWS Secrets Manager. ARN: {MaskedArn}", maskedArn);
            throw;
        }
    }
}
