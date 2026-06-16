using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class StaffUserConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Identity.StaffUser>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Identity.StaffUser> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
