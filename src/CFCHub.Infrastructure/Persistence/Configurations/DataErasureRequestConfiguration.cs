using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class DataErasureRequestConfiguration : IEntityTypeConfiguration<DataErasureRequest>
{
    public void Configure(EntityTypeBuilder<DataErasureRequest> builder)
    {
        builder.ToTable("data_erasure_requests");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, v => new DataErasureRequestId(v));
            
        builder.Property(x => x.StudentId)
            .HasConversion(x => x.Value, v => new StudentId(v));
            
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(x => x.BlockReason)
            .HasMaxLength(500);
    }
}
