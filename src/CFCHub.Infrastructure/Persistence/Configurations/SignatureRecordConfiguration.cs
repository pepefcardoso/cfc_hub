using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Contracts;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class SignatureRecordConfiguration : IEntityTypeConfiguration<SignatureRecord>
{
    public void Configure(EntityTypeBuilder<SignatureRecord> builder)
    {
        builder.ToTable("signature_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SignatureRecordId(value));

        builder.Property(x => x.ContractId)
            .HasConversion(id => id.Value, value => new ContractId(value));
            
        builder.Property(x => x.SignatureHash)
            .HasMaxLength(256)
            .IsRequired();
            
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45)
            .IsRequired();
    }
}
