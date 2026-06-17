using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using Microsoft.Extensions.Logging;

namespace CFCHub.Workers.Outbox.Handlers;

public record WelcomeEmailRequested(string Email, string StudentName, string LoginUrl);

public class WelcomeEmailHandler : IOutboxMessageHandler<WelcomeEmailRequested>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<WelcomeEmailHandler> _logger;

    public WelcomeEmailHandler(IEmailService emailService, ILogger<WelcomeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(WelcomeEmailRequested payload, CancellationToken ct)
    {
        var templateData = new Dictionary<string, string>
        {
            { "student_name", payload.StudentName },
            { "login_url", payload.LoginUrl }
        };

        var message = new EmailMessage("cfchub-welcome", payload.Email, templateData);
        await _emailService.SendAsync(message, ct);

        _logger.LogInformation("Welcome email queued for {Email}", payload.Email);
    }
}
