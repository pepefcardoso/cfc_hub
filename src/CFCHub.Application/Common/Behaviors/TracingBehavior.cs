using CFCHub.Application.Common.Telemetry;
using MediatR;

namespace CFCHub.Application.Common.Behaviors;

public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = AppActivitySource.Instance.StartActivity($"Handle {requestName}");

        return await next();
    }
}
