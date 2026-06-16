using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;
using MediatR;

namespace CFCHub.Application.Compliance.Commands.RegisterDocument;

public class RegisterDocumentCommandHandler : IRequestHandler<RegisterDocumentCommand, string?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IIdGenerator idGenerator,
        IFileStorageService fileStorageService,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _idGenerator = idGenerator;
        _fileStorageService = fileStorageService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<string?> Handle(RegisterDocumentCommand request, CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);

        var documentRecord = DocumentRecord.Create(
            studentId,
            request.Type,
            request.ExpiryDate,
            _idGenerator);

        await _documentRepository.AddAsync(documentRecord, cancellationToken);

        string? uploadUrl = null;
        if (request.Type == DocumentType.MedicalExam)
        {
            var objectKey = $"medical/{request.StudentId}/{documentRecord.Id.Value}.pdf";
            var presignedUrl = await _fileStorageService.GenerateUploadUrlAsync(
                StorageTarget.Medical,
                _tenantContext.TenantSlug,
                objectKey,
                "application/pdf",
                cancellationToken);

            uploadUrl = presignedUrl.Url;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return uploadUrl;
    }
}
