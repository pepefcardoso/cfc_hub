using System;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.ExternalServices.Detran.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace CFCHub.Infrastructure.ExternalServices.Detran;

public interface IStateDetranAdapterFactory
{
    IDetranAdapter GetAdapter(BrazilianState state);
}

public class StateDetranAdapterFactory : IStateDetranAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public StateDetranAdapterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDetranAdapter GetAdapter(BrazilianState state)
    {
        return state switch
        {
            BrazilianState.SP => _serviceProvider.GetRequiredService<SpDetranAdapter>(),
            BrazilianState.MG => _serviceProvider.GetRequiredService<MgDetranAdapter>(),
            _ => _serviceProvider.GetRequiredService<DefaultDetranAdapter>()
        };
    }
}
