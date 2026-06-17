using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using CFCHub.Domain.Identity;
using CFCHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CFCHub.Workers.Outbox.Handlers;

public record DocumentExpiryAlertRequested(
    string TenantId,
    Guid StudentId,
    string StudentName,
    string DocumentType,
    string AlertTier);

public class DocumentExpiryAlertHandler : IOutboxMessageHandler<DocumentExpiryAlertRequested>
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<DocumentExpiryAlertHandler> _logger;

    public DocumentExpiryAlertHandler(
        AppDbContext dbContext,
        IEmailService emailService,
        ILogger<DocumentExpiryAlertHandler> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(DocumentExpiryAlertRequested payload, CancellationToken ct)
    {
        var staffUsers = await _dbContext.StaffUsers
            .Where(u => u.Role == RoleType.Admin || u.Role == RoleType.Receptionist)
            .ToListAsync(ct);

        if (!staffUsers.Any())
        {
            _logger.LogWarning("No staff found to receive document expiry alert for {TenantId}", payload.TenantId);
            return;
        }

        string templateId = payload.AlertTier == "D30" ? "cfchub-doc-expiry-d30" : "cfchub-doc-expiry-d7";

        var templateData = new Dictionary<string, string>
        {
            { "student_name", payload.StudentName },
            { "document_type", payload.DocumentType }
        };

        foreach (var user in staffUsers)
        {
            var message = new EmailMessage(templateId, user.Email, templateData);
            await _emailService.SendAsync(message, ct);
        }

        _logger.LogInformation("Sent {TemplateId} alerts to {Count} staff users for student {StudentId}", templateId, staffUsers.Count, payload.StudentId);
    }
}
