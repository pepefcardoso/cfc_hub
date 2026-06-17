using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Models;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Workers.Pdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;

namespace CFCHub.Workers.Outbox.Handlers;

public record ContractGenerationRequested(
    Guid ContractId,
    string TenantId,
    string StudentName,
    string StudentEmail,
    DateTimeOffset EnrollmentDate,
    string CnhCategory,
    decimal TotalAmount,
    string TemplateKey,
    string PolicyVersion,
    string PolicyContentHash);

public class ContractGenerationHandler : IOutboxMessageHandler<ContractGenerationRequested>
{
    private readonly AppDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISystemClock _clock;
    private readonly ILogger<ContractGenerationHandler> _logger;

    public ContractGenerationHandler(
        AppDbContext dbContext,
        IFileStorageService fileStorageService,
        ISystemClock clock,
        ILogger<ContractGenerationHandler> logger)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(ContractGenerationRequested payload, CancellationToken ct)
    {
        var contractId = new ContractId(payload.ContractId);
        
        var contract = await _dbContext.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, ct);
        if (contract == null)
        {
            _logger.LogError("Contract {ContractId} not found in database.", payload.ContractId);
            throw new InvalidOperationException($"Contract {payload.ContractId} not found.");
        }

        var document = new ContractDocument(payload);
        var pdfBytes = document.GeneratePdf();

        var year = _clock.UtcNow.Year;
        var s3Key = $"contracts/{year}/{payload.ContractId}.pdf";

        using var pdfStream = new MemoryStream(pdfBytes);
        
        await _fileStorageService.UploadAsync(
            StorageTarget.Documents,
            payload.TenantId,
            s3Key,
            pdfStream,
            "application/pdf",
            ct);

        contract.MarkGenerated(s3Key);

        var notificationPayload = new 
        { 
            ContractId = payload.ContractId, 
            StudentEmail = payload.StudentEmail, 
            StudentName = payload.StudentName, 
            TenantId = payload.TenantId 
        };
        
        var outboxMessage = OutboxMessage.Create("ContractReadyNotified", JsonSerializer.Serialize(notificationPayload), _clock.UtcNow);
        _dbContext.OutboxMessages.Add(outboxMessage);

        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Contract {ContractId} generated and uploaded for tenant {TenantId}.", payload.ContractId, payload.TenantId);
    }
}
