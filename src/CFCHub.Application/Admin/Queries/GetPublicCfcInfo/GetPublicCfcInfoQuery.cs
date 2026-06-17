using CFCHub.Domain.Shared;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Admin.Queries.GetPublicCfcInfo;

public record PublicCfcInfoResult(string Name, string Document, string Address, string Phone);

public record GetPublicCfcInfoQuery(string Slug) : IRequest<Result<PublicCfcInfoResult>>;

public class GetPublicCfcInfoQueryHandler : IRequestHandler<GetPublicCfcInfoQuery, Result<PublicCfcInfoResult>>
{
    public Task<Result<PublicCfcInfoResult>> Handle(GetPublicCfcInfoQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<PublicCfcInfoResult>.Success(new PublicCfcInfoResult("CFC Demo", "123", "Rua A", "1199999999")));
    }
}
