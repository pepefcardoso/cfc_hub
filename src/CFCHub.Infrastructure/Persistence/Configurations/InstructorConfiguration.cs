using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Scheduling.Instructor>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Scheduling.Instructor> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
