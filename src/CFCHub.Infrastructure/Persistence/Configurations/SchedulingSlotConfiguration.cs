using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class SchedulingSlotConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Scheduling.SchedulingSlot>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Scheduling.SchedulingSlot> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
