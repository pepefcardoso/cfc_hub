using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CFCHub.Infrastructure.ExternalServices.Detran;

public class DetranHttpClient : IDetranClient
{
    private readonly IStateDetranAdapterFactory _adapterFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DetranHttpClient> _logger;
    private readonly string _env;
    private readonly BrazilianState _tenantState;

    public DetranHttpClient(
        IStateDetranAdapterFactory adapterFactory,
        IConnectionMultiplexer redis,
        ITenantContext tenantContext,
        IConfiguration configuration,
        ILogger<DetranHttpClient> logger)
    {
        _adapterFactory = adapterFactory;
        _redis = redis;
        _tenantContext = tenantContext;
        _logger = logger;
        _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        if (Enum.TryParse<BrazilianState>(configuration["Detran:State"], out var state))
        {
            _tenantState = state;
        }
        else
        {
            _tenantState = BrazilianState.SP;
        }
    }

    public async Task<CnhStatusResult> GetCnhStatusAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var cpfHash = RedisKeys.CpfHash(cpf);
        var cacheKey = RedisKeys.DetranCnhStatus(_env, _tenantContext.TenantSlug, cpfHash);

        var cachedResult = await db.StringGetAsync(cacheKey);
        if (cachedResult.HasValue)
        {
            var cachedObj = JsonSerializer.Deserialize<CnhStatusResult>(cachedResult.ToString());
            if (cachedObj != null)
            {
                return cachedObj;
            }
        }

        var adapter = _adapterFactory.GetAdapter(_tenantState);
        CnhStatusResult result;

        try
        {
            result = await adapter.GetCnhStatusAsync(cpf, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Adapter failure when fetching CNH status for tenant {Tenant}", _tenantContext.TenantSlug);
            return CnhStatusResult.Unavailable;
        }

        if (result != CnhStatusResult.Unavailable)
        {
            var json = JsonSerializer.Serialize(result);
            await db.StringSetAsync(cacheKey, json, TimeSpan.FromSeconds(86400));
        }

        return result;
    }
}
