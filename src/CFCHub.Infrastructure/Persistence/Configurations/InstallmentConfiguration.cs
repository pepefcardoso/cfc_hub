using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("installments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new InstallmentId(value));

        builder.Property(x => x.EnrollmentId)
            .HasConversion(id => id.Value, value => new EnrollmentId(value));

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasColumnType("NUMERIC(18,2)");

            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasColumnType("TEXT");
        });

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasColumnType("TEXT");
    }
}
