using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Compliance;
using MediatR;

namespace CFCHub.Application.Compliance.Queries.GetExpiringDocuments;

public class GetExpiringDocumentsQueryHandler : IRequestHandler<GetExpiringDocumentsQuery, CFCHub.Domain.Shared.PagedResult<ExpiringDocumentResult>>
{
    private readonly IDocumentRepository _documentRepository;

    public GetExpiringDocumentsQueryHandler(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<CFCHub.Domain.Shared.PagedResult<ExpiringDocumentResult>> Handle(GetExpiringDocumentsQuery request, CancellationToken cancellationToken)
    {
        var records = await _documentRepository.GetExpiringAsync(request.From, request.To, cancellationToken);

        var list = records.Select(r => new ExpiringDocumentResult(
            r.Id.Value,
            r.StudentId.Value,
            r.Type,
            r.ExpiryDate,
            r.LastAlertSentAt
        )).ToList();
        
        return new CFCHub.Domain.Shared.PagedResult<ExpiringDocumentResult>(list, null, false, list.Count);
    }
}
