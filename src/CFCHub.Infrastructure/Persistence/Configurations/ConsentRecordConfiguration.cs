using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class ConsentRecordConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Enrollment.ConsentRecord>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Enrollment.ConsentRecord> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
