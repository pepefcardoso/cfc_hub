using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Students;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractId(value));

        builder.Property(x => x.StudentId)
            .HasConversion(id => id.Value, value => new CFCHub.Domain.Enrollment.StudentId(value));

        builder.Property(x => x.EnrollmentId)
            .HasConversion(id => id.Value, value => new CFCHub.Domain.Enrollment.EnrollmentId(value));

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasColumnType("TEXT");

        builder.HasOne(x => x.Signature)
            .WithOne()
            .HasForeignKey<SignatureRecord>(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
