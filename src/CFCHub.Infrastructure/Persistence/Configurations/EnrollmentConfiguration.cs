using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Enrollment.Enrollment>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Enrollment.Enrollment> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
