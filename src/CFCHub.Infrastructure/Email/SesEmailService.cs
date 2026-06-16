using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using CFCHub.Application.Common.Email;
using CFCHub.Domain.Shared.Exceptions;

namespace CFCHub.Infrastructure.Email;

public class SesEmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    
    private static readonly HashSet<string> AllowedTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "cfchub-welcome",
        "cfchub-slot-reminder",
        "cfchub-contract-ready",
        "cfchub-payment-receipt",
        "cfchub-doc-expiry-d30",
        "cfchub-doc-expiry-d7",
        "cfchub-erasure-complete"
    };

    private static readonly HashSet<string> ForbiddenDataKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "cpf", "rg", "medical", "address"
    };

    public SesEmailService(IAmazonSimpleEmailService sesClient)
    {
        _sesClient = sesClient;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.ToAddress))
        {
            throw new ArgumentException("ToAddress cannot be empty.", nameof(message));
        }

        if (!AllowedTemplates.Contains(message.TemplateId))
        {
            throw new ArgumentException($"Template ID '{message.TemplateId}' is not allowed.");
        }

        if (message.TemplateData != null)
        {
            foreach (var key in message.TemplateData.Keys)
            {
                if (ForbiddenDataKeys.Any(fk => key.Contains(fk, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"Template data key '{key}' contains potentially sensitive information which is forbidden in emails.");
                }
            }
        }

        try
        {
            var sourceEmail = Environment.GetEnvironmentVariable("CFCHUB_SES_FROM_ADDRESS") ?? "noreply@cfchub.com.br";
            
            var sendRequest = new SendTemplatedEmailRequest
            {
                Source = sourceEmail,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { message.ToAddress }
                },
                Template = message.TemplateId,
                TemplateData = message.TemplateData != null ? JsonSerializer.Serialize(message.TemplateData) : "{}"
            };

            await _sesClient.SendTemplatedEmailAsync(sendRequest, ct);
        }
        catch (AmazonSimpleEmailServiceException ex)
        {
            throw new EmailDeliveryException($"Failed to send email via SES for template {message.TemplateId}. Reason: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new EmailDeliveryException($"An unexpected error occurred while sending email for template {message.TemplateId}. Reason: {ex.Message}");
        }
    }
}
