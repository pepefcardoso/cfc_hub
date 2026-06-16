using CFCHub.Domain.Enrollment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        builder.ToTable("consent_records");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ConsentRecordId(value));

        builder.Property(x => x.StudentId)
            .HasConversion(id => id.Value, value => new StudentId(value))
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();
            
        builder.Ignore(x => x.DomainEvents);
    }
}
