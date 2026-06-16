using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Enrollment.Student>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Enrollment.Student> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
