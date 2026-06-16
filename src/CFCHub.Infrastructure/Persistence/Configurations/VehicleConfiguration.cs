using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Scheduling.Vehicle>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Scheduling.Vehicle> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
