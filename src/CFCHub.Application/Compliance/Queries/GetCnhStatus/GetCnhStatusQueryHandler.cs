using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Compliance;
using MediatR;

namespace CFCHub.Application.Compliance.Queries.GetCnhStatus;

public class GetCnhStatusQueryHandler : IRequestHandler<GetCnhStatusQuery, CnhStatusResult>
{
    private readonly IDetranClient _detranClient;

    public GetCnhStatusQueryHandler(IDetranClient detranClient)
    {
        _detranClient = detranClient;
    }

    public async Task<CnhStatusResult> Handle(GetCnhStatusQuery request, CancellationToken cancellationToken)
    {
        var domainResult = await _detranClient.GetCnhStatusAsync(request.Cpf, cancellationToken);
        
        if (!domainResult.IsAvailable)
        {
            return new CnhStatusResult(false, "Consultar manualmente", null, null);
        }

        return new CnhStatusResult(
            domainResult.IsAvailable,
            domainResult.Status,
            domainResult.ExpiryDate,
            domainResult.Points);
    }
}
