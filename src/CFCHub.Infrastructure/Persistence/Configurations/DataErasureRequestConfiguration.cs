using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Identity;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class DataErasureRequestConfiguration : IEntityTypeConfiguration<DataErasureRequest>
{
    public void Configure(EntityTypeBuilder<DataErasureRequest> builder)
    {
        builder.ToTable("data_erasure_requests");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Status)
            .HasMaxLength(50);
    }
}
