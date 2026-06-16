using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Contracts.Commands.SignContract;

public class SignContractCommandHandler : IRequestHandler<SignContractCommand>
{
    private readonly IContractRepository _contractRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;

    public SignContractCommandHandler(
        IContractRepository contractRepository,
        ICurrentUserService currentUserService,
        ISystemClock clock,
        IIdGenerator idGenerator)
    {
        _contractRepository = contractRepository;
        _currentUserService = currentUserService;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task Handle(SignContractCommand request, CancellationToken cancellationToken)
    {
        var contractId = new ContractId(request.ContractId);
        var contract = await _contractRepository.GetByIdAsync(contractId, cancellationToken);

        if (contract == null)
        {
            throw new NotFoundException($"Contract {request.ContractId} not found.");
        }

        var signatureRecordId = _idGenerator.NewId<SignatureRecordId>();
        var signatureRecord = new SignatureRecord(
            signatureRecordId,
            contractId,
            request.SignatureHash,
            _currentUserService.IpAddress,
            _clock.UtcNow);

        contract.Sign(signatureRecord, _clock);

        await _contractRepository.UpdateAsync(contract, cancellationToken);
    }
}
