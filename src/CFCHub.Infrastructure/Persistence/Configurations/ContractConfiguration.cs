using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<CFCHub.Domain.Contracts.Contract>
{
    public void Configure(EntityTypeBuilder<CFCHub.Domain.Contracts.Contract> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
