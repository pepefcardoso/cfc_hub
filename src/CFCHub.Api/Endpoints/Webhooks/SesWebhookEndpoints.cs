using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Util;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared.Email;
using CFCHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace CFCHub.Api.Endpoints.Webhooks;

public static class SesWebhookEndpoints
{
    public static void MapSesWebhooks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/ses/events", async (HttpContext context, ILoggerFactory loggerFactory, AppDbContext dbContext, ITenantContext tenantContext) =>
        {
            var logger = loggerFactory.CreateLogger("SesWebhookEndpoints");

            // Webhooks might not have a tenant resolved via JWT. Set to a public context for saving logs to the public schema
            if (!tenantContext.IsResolved && tenantContext is TenantContext tc)
            {
                tc.Resolve("public", "global", Guid.Empty);
            }

            try
            {
                string requestBody;
                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                // Verify SNS Header
                if (!context.Request.Headers.TryGetValue("x-amz-sns-message-type", out var messageTypeHeader))
                {
                    logger.LogWarning("Missing x-amz-sns-message-type header from {IpAddress}", context.Connection.RemoteIpAddress);
                    return Results.BadRequest();
                }

                // Verify SNS signature
                var snsMessage = Message.ParseMessage(requestBody);
                if (!snsMessage.IsMessageSignatureValid())
                {
                    logger.LogWarning("Invalid SNS signature from {IpAddress}", context.Connection.RemoteIpAddress);
                    return Results.StatusCode(403);
                }

                if (snsMessage.Type.Equals("SubscriptionConfirmation", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("SNS Subscription Confirmation. SubscribeURL: {Url}", snsMessage.SubscribeURL);
                    return Results.Ok();
                }

                if (snsMessage.Type.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    var messageStr = snsMessage.MessageText;
                    if (string.IsNullOrEmpty(messageStr)) return Results.Ok();

                    var sesEvent = JsonNode.Parse(messageStr);
                    var notificationType = sesEvent?["notificationType"]?.GetValue<string>();
                    
                    var mailNode = sesEvent?["mail"];
                    var sesMessageId = mailNode?["messageId"]?.GetValue<string>() ?? Guid.NewGuid().ToString();
                    var destination = mailNode?["destination"]?[0]?.GetValue<string>() ?? string.Empty;
                    
                    string? bounceType = null;
                    string? eventTimestampStr = null;

                    if (notificationType == "Bounce")
                    {
                        var bounceNode = sesEvent?["bounce"];
                        bounceType = bounceNode?["bounceType"]?.GetValue<string>();
                        eventTimestampStr = bounceNode?["timestamp"]?.GetValue<string>();
                    }
                    else if (notificationType == "Complaint")
                    {
                        var complaintNode = sesEvent?["complaint"];
                        bounceType = complaintNode?["complaintFeedbackType"]?.GetValue<string>();
                        eventTimestampStr = complaintNode?["timestamp"]?.GetValue<string>();
                    }
                    else if (notificationType == "Delivery")
                    {
                        eventTimestampStr = sesEvent?["delivery"]?["timestamp"]?.GetValue<string>();
                    }

                    // Fallback to mail timestamp if event timestamp is missing
                    if (string.IsNullOrEmpty(eventTimestampStr))
                    {
                        eventTimestampStr = mailNode?["timestamp"]?.GetValue<string>();
                    }

                    DateTimeOffset occurredAt = DateTimeOffset.TryParse(eventTimestampStr, out var parsedDate) ? parsedDate : DateTimeOffset.UtcNow;

                    string destinationHash;
                    using (var sha256 = SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(destination.ToLowerInvariant()));
                        destinationHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
                    }

                    var log = EmailDeliveryLog.Create(
                        id: new EmailDeliveryLogId(Guid.NewGuid()),
                        sesMessageId: sesMessageId,
                        eventType: notificationType ?? "Unknown",
                        recipientAddressHash: destinationHash,
                        occurredAt: occurredAt,
                        bounceType: bounceType
                    );

                    dbContext.EmailDeliveryLogs.Add(log);
                    await dbContext.SaveChangesAsync();
                }

                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing SES webhook");
                return Results.StatusCode(500);
            }
        }).AllowAnonymous();
    }
}
