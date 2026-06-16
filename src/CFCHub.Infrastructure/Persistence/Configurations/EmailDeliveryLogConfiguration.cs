using CFCHub.Domain.Shared.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class EmailDeliveryLogConfiguration : IEntityTypeConfiguration<EmailDeliveryLog>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryLog> builder)
    {
        builder.ToTable("email_delivery_logs", "public");

        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new EmailDeliveryLogId(value));
            
        builder.Property(e => e.SesMessageId).HasColumnName("ses_message_id").IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
        builder.Property(e => e.RecipientAddressHash).HasColumnName("recipient_address_hash").IsRequired();
        builder.Property(e => e.BounceType).HasColumnName("bounce_type");
        builder.Property(e => e.Timestamp).HasColumnName("timestamp");
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");
    }
}
