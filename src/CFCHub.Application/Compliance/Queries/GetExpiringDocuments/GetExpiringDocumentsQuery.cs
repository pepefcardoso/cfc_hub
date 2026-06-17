using System;
using System.Collections.Generic;
using MediatR;

namespace CFCHub.Application.Compliance.Queries.GetExpiringDocuments;

public record GetExpiringDocumentsQuery(DateOnly From, DateOnly To, int Limit = 20, string? Cursor = null) : IRequest<CFCHub.Domain.Shared.PagedResult<ExpiringDocumentResult>>;
