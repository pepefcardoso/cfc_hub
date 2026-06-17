using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Shared.Outbox;
using System;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OutboxMessageId(value))
            .HasColumnName("id");

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(s => s.ToString(), s => Enum.Parse<OutboxMessageStatus>(s))
            .HasColumnType("TEXT")
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.Attempts)
            .HasColumnName("attempts")
            .IsRequired();

        builder.Property(x => x.MaxAttempts)
            .HasColumnName("max_attempts")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ScheduledAfter)
            .HasColumnName("scheduled_after")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Error)
            .HasColumnName("error");

        builder.Property(x => x.ErrorDetails)
            .HasColumnType("jsonb")
            .HasColumnName("error_details");

        builder.HasIndex(x => new { x.Status, x.ScheduledAfter })
            .HasDatabaseName("idx_outbox_pending")
            .HasFilter("status = 'Pending'");
    }
}
