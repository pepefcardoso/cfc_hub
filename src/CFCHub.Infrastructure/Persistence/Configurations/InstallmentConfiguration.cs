using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class InstallmentConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Finance.Installment>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Finance.Installment> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
