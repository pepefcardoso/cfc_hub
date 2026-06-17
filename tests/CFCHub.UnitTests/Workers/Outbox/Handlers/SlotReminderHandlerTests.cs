using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using CFCHub.Workers.Outbox.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Workers.Outbox.Handlers;

public class SlotReminderHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDeserializePayloadAndSendCorrectEmail()
    {
        var json = """
            {
                "Email": "student@example.com",
                "StudentName": "John Doe",
                "SlotDate": "2023-10-15 10:00",
                "InstructorName": "Jane Smith",
                "CfcAddress": "123 Main St"
            }
            """;
        var payload = JsonSerializer.Deserialize<SlotReminderRequested>(json);

        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<SlotReminderHandler>>();

        var handler = new SlotReminderHandler(emailService, logger);

        await handler.HandleAsync(payload!, CancellationToken.None);

        payload.Should().NotBeNull();

        await emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => 
                m.TemplateId == "cfchub-slot-reminder" &&
                m.ToAddress == "student@example.com" &&
                m.TemplateData["student_name"] == "John Doe" &&
                m.TemplateData["slot_date"] == "2023-10-15 10:00" &&
                m.TemplateData["instructor_name"] == "Jane Smith" &&
                m.TemplateData["cfc_address"] == "123 Main St" &&
                !m.TemplateData.ContainsKey("cpf")
            ),
            CancellationToken.None);
    }
}
