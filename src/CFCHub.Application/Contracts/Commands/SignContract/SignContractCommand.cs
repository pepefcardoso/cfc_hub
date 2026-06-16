using System;

namespace CFCHub.Application.Contracts.Commands.SignContract;

public record SignContractCommand(Guid ContractId, string SignatureHash) : MediatR.IRequest;
