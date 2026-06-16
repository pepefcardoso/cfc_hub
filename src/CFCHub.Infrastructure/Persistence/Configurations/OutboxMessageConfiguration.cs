using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<CFCHub.Infrastructure.Outbox.OutboxMessage>
{
    public void Configure(EntityTypeBuilder<CFCHub.Infrastructure.Outbox.OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
