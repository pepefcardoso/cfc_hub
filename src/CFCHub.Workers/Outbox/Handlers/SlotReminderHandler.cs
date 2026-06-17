using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using Microsoft.Extensions.Logging;

namespace CFCHub.Workers.Outbox.Handlers;

public record SlotReminderRequested(string Email, string StudentName, string SlotDate, string InstructorName, string CfcAddress);

public class SlotReminderHandler : IOutboxMessageHandler<SlotReminderRequested>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SlotReminderHandler> _logger;

    public SlotReminderHandler(IEmailService emailService, ILogger<SlotReminderHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(SlotReminderRequested payload, CancellationToken ct)
    {
        var templateData = new Dictionary<string, string>
        {
            { "student_name", payload.StudentName },
            { "slot_date", payload.SlotDate },
            { "instructor_name", payload.InstructorName },
            { "cfc_address", payload.CfcAddress }
        };

        var message = new EmailMessage("cfchub-slot-reminder", payload.Email, templateData);
        await _emailService.SendAsync(message, ct);

        _logger.LogInformation("Slot reminder email queued for {Email} on {SlotDate}", payload.Email, payload.SlotDate);
    }
}
