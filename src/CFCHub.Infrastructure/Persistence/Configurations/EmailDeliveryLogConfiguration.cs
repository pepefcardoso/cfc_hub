using CFCHub.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class EmailDeliveryLogConfiguration : IEntityTypeConfiguration<EmailDeliveryLog>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryLog> builder)
    {
        builder.ToTable("email_delivery_logs", "public");

        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.MessageId).HasColumnName("message_id").IsRequired();
        builder.Property(e => e.NotificationType).HasColumnName("notification_type").IsRequired();
        builder.Property(e => e.DestinationAddress).HasColumnName("destination_address").IsRequired();
        builder.Property(e => e.StatusDetails).HasColumnName("status_details");
        builder.Property(e => e.Timestamp).HasColumnName("timestamp");
    }
}
