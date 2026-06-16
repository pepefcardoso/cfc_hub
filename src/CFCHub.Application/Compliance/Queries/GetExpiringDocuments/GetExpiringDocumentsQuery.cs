using System;
using System.Collections.Generic;
using MediatR;

namespace CFCHub.Application.Compliance.Queries.GetExpiringDocuments;

public record GetExpiringDocumentsQuery(DateOnly From, DateOnly To) : IRequest<IEnumerable<ExpiringDocumentResult>>;
