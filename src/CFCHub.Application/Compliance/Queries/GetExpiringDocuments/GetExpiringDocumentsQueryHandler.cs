using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Compliance;
using MediatR;

namespace CFCHub.Application.Compliance.Queries.GetExpiringDocuments;

public class GetExpiringDocumentsQueryHandler : IRequestHandler<GetExpiringDocumentsQuery, IEnumerable<ExpiringDocumentResult>>
{
    private readonly IDocumentRepository _documentRepository;

    public GetExpiringDocumentsQueryHandler(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<IEnumerable<ExpiringDocumentResult>> Handle(GetExpiringDocumentsQuery request, CancellationToken cancellationToken)
    {
        var records = await _documentRepository.GetExpiringAsync(request.From, request.To, cancellationToken);

        return records.Select(r => new ExpiringDocumentResult(
            r.Id.Value,
            r.StudentId.Value,
            r.Type,
            r.ExpiryDate,
            r.LastAlertSentAt
        ));
    }
}
