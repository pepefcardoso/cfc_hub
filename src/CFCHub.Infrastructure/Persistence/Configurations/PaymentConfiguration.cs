using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Students;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PaymentId(value));

        builder.Property(x => x.StudentId)
            .HasConversion(id => id.Value, value => new CFCHub.Domain.Enrollment.StudentId(value));

        builder.Property(x => x.EnrollmentId)
            .HasConversion(id => id.Value, value => new CFCHub.Domain.Enrollment.EnrollmentId(value));

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

        builder.Property(x => x.Method)
            .HasConversion<string>()
            .HasColumnType("TEXT");

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedBy);
    }
}
