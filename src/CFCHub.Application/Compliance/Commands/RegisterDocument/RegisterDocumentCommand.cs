using System;
using CFCHub.Domain.Compliance;
using MediatR;

namespace CFCHub.Application.Compliance.Commands.RegisterDocument;

public record RegisterDocumentCommand(
    Guid StudentId,
    DocumentType Type,
    DateOnly ExpiryDate
) : IRequest<string?>;
