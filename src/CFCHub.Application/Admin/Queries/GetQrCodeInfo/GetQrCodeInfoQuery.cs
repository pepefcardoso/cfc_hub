using CFCHub.Domain.Shared;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Admin.Queries.GetQrCodeInfo;

public record QrCodeInfoResult(string DocumentType, string Url);

public record GetQrCodeInfoQuery(string Code) : IRequest<Result<QrCodeInfoResult>>;

public class GetQrCodeInfoQueryHandler : IRequestHandler<GetQrCodeInfoQuery, Result<QrCodeInfoResult>>
{
    public Task<Result<QrCodeInfoResult>> Handle(GetQrCodeInfoQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<QrCodeInfoResult>.Success(new QrCodeInfoResult("Contract", "http://example.com")));
    }
}
