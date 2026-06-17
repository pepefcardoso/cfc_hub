using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using Microsoft.Extensions.Logging;

namespace CFCHub.Workers.Outbox.Handlers;

public record ErasureNotificationRequested(string Email, string ReferenceNumber);

public class ErasureNotificationHandler : IOutboxMessageHandler<ErasureNotificationRequested>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ErasureNotificationHandler> _logger;

    public ErasureNotificationHandler(IEmailService emailService, ILogger<ErasureNotificationHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(ErasureNotificationRequested payload, CancellationToken ct)
    {
        var templateData = new Dictionary<string, string>
        {
            { "reference_number", payload.ReferenceNumber }
        };

        var message = new EmailMessage("cfchub-erasure-complete", payload.Email, templateData);
        await _emailService.SendAsync(message, ct);

        _logger.LogInformation("Erasure completion notification queued for {Email} with ref {ReferenceNumber}", payload.Email, payload.ReferenceNumber);
    }
}
