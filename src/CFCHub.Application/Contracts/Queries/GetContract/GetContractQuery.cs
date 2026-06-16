using System;

namespace CFCHub.Application.Contracts.Queries.GetContract;

public record GetContractQuery(Guid ContractId) : MediatR.IRequest<ContractResult>;
