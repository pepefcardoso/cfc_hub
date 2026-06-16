using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Finance.Payment>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Finance.Payment> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
