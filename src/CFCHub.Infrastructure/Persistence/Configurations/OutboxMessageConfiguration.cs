using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Infrastructure.Outbox;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb");

        builder.Property(x => x.Status)
            .HasColumnType("TEXT");

        builder.HasIndex(x => x.Status)
            .HasAnnotation("Relational:Filter", "status = 'Pending'");
    }
}
