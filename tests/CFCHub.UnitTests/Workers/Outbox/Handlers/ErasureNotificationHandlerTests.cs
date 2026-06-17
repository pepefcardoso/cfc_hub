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

public class ErasureNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDeserializePayloadAndSendCorrectEmail()
    {
        var json = """
            {
                "Email": "student@example.com",
                "ReferenceNumber": "REF-12345"
            }
            """;
        var payload = JsonSerializer.Deserialize<ErasureNotificationRequested>(json);

        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<ErasureNotificationHandler>>();

        var handler = new ErasureNotificationHandler(emailService, logger);

        await handler.HandleAsync(payload!, CancellationToken.None);

        payload.Should().NotBeNull();

        await emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => 
                m.TemplateId == "cfchub-erasure-complete" &&
                m.ToAddress == "student@example.com" &&
                m.TemplateData["reference_number"] == "REF-12345" &&
                !m.TemplateData.ContainsKey("cpf") &&
                !m.TemplateData.ContainsKey("name")
            ),
            CancellationToken.None);
    }
}
