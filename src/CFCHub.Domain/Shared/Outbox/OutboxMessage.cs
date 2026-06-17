using System;

namespace CFCHub.Domain.Shared.Outbox;

public sealed class OutboxMessage
{
    public OutboxMessageId Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; } = 5;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ScheduledAfter { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorDetails { get; private set; }

    private OutboxMessage() { } // EF Core

    public static OutboxMessage Create(string type, string payload, DateTimeOffset now, int maxAttempts = 5)
    {
        return new OutboxMessage
        {
            Id = OutboxMessageId.New(),
            Type = type,
            Payload = payload,
            Status = OutboxMessageStatus.Pending,
            Attempts = 0,
            MaxAttempts = maxAttempts,
            CreatedAt = now,
            ScheduledAfter = now
        };
    }

    public void MarkAsProcessing()
    {
        Status = OutboxMessageStatus.Processing;
    }

    public void MarkAsProcessed(DateTimeOffset now)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = now;
    }

    public void MarkAsFailed(DateTimeOffset now, string error, string? errorDetails = null)
    {
        Attempts++;
        Error = error;

        if (errorDetails != null)
        {
            ErrorDetails = errorDetails;
        }

        if (Attempts >= MaxAttempts)
        {
            Status = OutboxMessageStatus.Failed;
        }
        else
        {
            Status = OutboxMessageStatus.Pending;
            // Exponential backoff: 2^attempts seconds
            ScheduledAfter = now.AddSeconds(Math.Pow(2, Attempts));
        }
    }
}
