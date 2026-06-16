using System;
using System.IO;
using System.Text.Json.Nodes;
using CFCHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

using CFCHub.Application.Common.Interfaces;

namespace CFCHub.Infrastructure.Email;

public static class SesEventWebhookHandler
{
    public static void MapSesWebhooks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/webhooks/ses/events", async (HttpContext context, ILogger<AppDbContext> logger, AppDbContext dbContext, ITenantContext tenantContext) =>
        {
            try
            {
                if (!tenantContext.IsResolved)
                {
                    if (tenantContext is TenantContext tc)
                    {
                        tc.Resolve("public", "global", Guid.Empty);
                    }
                }

                using var reader = new StreamReader(context.Request.Body);
                var requestBody = await reader.ReadToEndAsync();
                var snsMessage = JsonNode.Parse(requestBody);
                
                if (snsMessage == null) return Results.BadRequest();

                var type = snsMessage["Type"]?.GetValue<string>();
                if (type == "SubscriptionConfirmation")
                {
                    var subscribeUrl = snsMessage["SubscribeURL"]?.GetValue<string>();
                    logger.LogInformation("SNS Subscription URL: {Url}", subscribeUrl);
                    return Results.Ok();
                }

                if (type == "Notification")
                {
                    var messageStr = snsMessage["Message"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(messageStr)) return Results.Ok();

                    var sesEvent = JsonNode.Parse(messageStr);
                    var notificationType = sesEvent?["notificationType"]?.GetValue<string>();
                    
                    var mailNode = sesEvent?["mail"];
                    var messageId = mailNode?["messageId"]?.GetValue<string>() ?? Guid.NewGuid().ToString();
                    var destination = mailNode?["destination"]?[0]?.GetValue<string>() ?? string.Empty;

                    string? details = null;
                    if (notificationType == "Bounce")
                    {
                        details = sesEvent?["bounce"]?["bounceType"]?.GetValue<string>();
                    }
                    else if (notificationType == "Complaint")
                    {
                        details = sesEvent?["complaint"]?["complaintFeedbackType"]?.GetValue<string>();
                    }

                    var log = new EmailDeliveryLog
                    {
                        Id = Guid.NewGuid(),
                        MessageId = messageId,
                        NotificationType = notificationType ?? "Unknown",
                        DestinationAddress = destination,
                        StatusDetails = details,
                        Timestamp = DateTimeOffset.UtcNow
                    };

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
        });
    }
}
