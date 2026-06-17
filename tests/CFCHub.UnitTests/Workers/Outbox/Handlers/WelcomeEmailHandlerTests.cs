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

public class WelcomeEmailHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDeserializePayloadAndSendCorrectEmail()
    {
        var json = """
            {
                "Email": "student@example.com",
                "StudentName": "John Doe",
                "LoginUrl": "https://example.com/login"
            }
            """;
        var payload = JsonSerializer.Deserialize<WelcomeEmailRequested>(json);

        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<WelcomeEmailHandler>>();

        var handler = new WelcomeEmailHandler(emailService, logger);

        await handler.HandleAsync(payload!, CancellationToken.None);

        payload.Should().NotBeNull();
        payload!.Email.Should().Be("student@example.com");

        await emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => 
                m.TemplateId == "cfchub-welcome" &&
                m.ToAddress == "student@example.com" &&
                m.TemplateData["student_name"] == "John Doe" &&
                m.TemplateData["login_url"] == "https://example.com/login" &&
                !m.TemplateData.ContainsKey("cpf")
            ),
            CancellationToken.None);
    }
}
