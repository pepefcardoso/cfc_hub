using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Workers.Pdf;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;

namespace CFCHub.Workers.Outbox.Handlers;

public record PaymentReceiptRequested(
    Guid PaymentId, 
    string TenantId, 
    string StudentName, 
    string Email, 
    decimal Amount, 
    string PaymentMethod, 
    DateTimeOffset PaymentDate);

public class PaymentReceiptHandler : IOutboxMessageHandler<PaymentReceiptRequested>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IEmailService _emailService;
    private readonly ISystemClock _clock;
    private readonly ILogger<PaymentReceiptHandler> _logger;

    public PaymentReceiptHandler(
        IFileStorageService fileStorageService,
        IEmailService emailService,
        ISystemClock clock,
        ILogger<PaymentReceiptHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _emailService = emailService;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentReceiptRequested payload, CancellationToken ct)
    {
        var document = new PaymentReceiptDocument(payload);
        var pdfBytes = document.GeneratePdf();

        var year = _clock.UtcNow.Year;
        var s3Key = $"receipts/{year}/{payload.PaymentId}.pdf";

        using var pdfStream = new MemoryStream(pdfBytes);

        await _fileStorageService.UploadAsync(
            StorageTarget.Documents,
            payload.TenantId,
            s3Key,
            pdfStream,
            "application/pdf",
            ct);

        var receiptUrl = await _fileStorageService.GenerateDownloadUrlAsync(
            StorageTarget.Documents,
            payload.TenantId,
            s3Key,
            TimeSpan.FromHours(24),
            ct);

        var templateData = new Dictionary<string, string>
        {
            { "student_name", payload.StudentName },
            { "receipt_url", receiptUrl }
        };

        var message = new EmailMessage("cfchub-payment-receipt", payload.Email, templateData);
        await _emailService.SendAsync(message, ct);

        _logger.LogInformation("Payment receipt {PaymentId} generated and email queued for {Email}", payload.PaymentId, payload.Email);
    }
}
