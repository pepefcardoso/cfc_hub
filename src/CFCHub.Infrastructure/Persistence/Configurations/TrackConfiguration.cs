using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class TrackConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Scheduling.Track>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Scheduling.Track> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
