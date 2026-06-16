using CFCHub.Domain.Enrollment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new EnrollmentId(value));

        builder.Property(x => x.StudentId)
            .HasConversion(id => id.Value, value => new StudentId(value))
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();

        // Global soft-delete filter is handled by AppDbContext for ISoftDeletable entities
        // builder.HasQueryFilter(x => x.DeletedAt == null);
        
        builder.Ignore(x => x.DomainEvents);
    }
}
