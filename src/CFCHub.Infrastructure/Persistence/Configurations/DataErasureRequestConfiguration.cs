using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class DataErasureRequestConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Identity.DataErasureRequest>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Identity.DataErasureRequest> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
