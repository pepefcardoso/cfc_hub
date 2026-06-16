using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Contracts.Queries.GetContract;

public class GetContractQueryHandler : IRequestHandler<GetContractQuery, ContractResult>
{
    private readonly IContractRepository _contractRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITenantContext _tenantContext;

    public GetContractQueryHandler(
        IContractRepository contractRepository,
        IFileStorageService fileStorageService,
        ITenantContext tenantContext)
    {
        _contractRepository = contractRepository;
        _fileStorageService = fileStorageService;
        _tenantContext = tenantContext;
    }

    public async Task<ContractResult> Handle(GetContractQuery request, CancellationToken cancellationToken)
    {
        var contractId = new ContractId(request.ContractId);
        var contract = await _contractRepository.GetByIdAsync(contractId, cancellationToken);

        if (contract == null)
        {
            throw new NotFoundException($"Contract {request.ContractId} not found.");
        }

        string? downloadUrl = null;
        if (!string.IsNullOrEmpty(contract.S3Key))
        {
            downloadUrl = await _fileStorageService.GenerateDownloadUrlAsync(
                StorageTarget.Documents,
                _tenantContext.TenantSlug,
                contract.S3Key,
                TimeSpan.FromSeconds(3600),
                cancellationToken);
        }

        return new ContractResult(
            contract.Id.Value,
            contract.StudentId.Value,
            contract.EnrollmentId.Value,
            contract.Status.ToString(),
            downloadUrl,
            contract.SignedAt,
            contract.CreatedAt);
    }
}
