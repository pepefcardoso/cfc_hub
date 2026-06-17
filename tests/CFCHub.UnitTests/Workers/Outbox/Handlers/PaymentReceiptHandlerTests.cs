using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Email;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Workers.Outbox.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Workers.Outbox.Handlers;

public class PaymentReceiptHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDeserializePayloadGeneratePdfAndSendEmail()
    {
        CFCHub.Workers.Pdf.PdfConfiguration.Configure();

        var paymentId = Guid.NewGuid();
        var json = $$"""
            {
                "PaymentId": "{{paymentId}}",
                "TenantId": "tenant1",
                "StudentName": "John Doe",
                "Email": "student@example.com",
                "Amount": 150.00,
                "PaymentMethod": "PIX",
                "PaymentDate": "2023-10-15T10:00:00Z"
            }
            """;
        var payload = JsonSerializer.Deserialize<PaymentReceiptRequested>(json);

        var fileStorage = Substitute.For<IFileStorageService>();
        var emailService = Substitute.For<IEmailService>();
        var clock = Substitute.For<ISystemClock>();
        var logger = Substitute.For<ILogger<PaymentReceiptHandler>>();

        clock.UtcNow.Returns(new DateTimeOffset(2023, 10, 15, 10, 0, 0, TimeSpan.Zero));
        fileStorage.GenerateDownloadUrlAsync(StorageTarget.Documents, "tenant1", Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns("https://s3.example.com/receipt.pdf");

        var handler = new PaymentReceiptHandler(fileStorage, emailService, clock, logger);

        await handler.HandleAsync(payload!, CancellationToken.None);

        payload.Should().NotBeNull();
        payload!.Amount.Should().Be(150.00m);

        await emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => 
                m.TemplateId == "cfchub-payment-receipt" &&
                m.ToAddress == "student@example.com" &&
                m.TemplateData["student_name"] == "John Doe" &&
                m.TemplateData["receipt_url"] == "https://s3.example.com/receipt.pdf" &&
                !m.TemplateData.ContainsKey("amount")
            ),
            CancellationToken.None);
    }
}
