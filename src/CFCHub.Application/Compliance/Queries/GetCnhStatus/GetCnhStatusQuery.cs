using MediatR;

namespace CFCHub.Application.Compliance.Queries.GetCnhStatus;

public record GetCnhStatusQuery(string Cpf) : IRequest<CnhStatusResult>;
