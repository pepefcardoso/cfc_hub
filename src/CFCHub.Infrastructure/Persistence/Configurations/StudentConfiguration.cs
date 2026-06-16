using CFCHub.Domain.Enrollment;
using CFCHub.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new StudentId(value));

        builder.Property(x => x.Name)
            .HasConversion<EncryptedStringConverter>()
            .IsRequired();

        builder.Property(x => x.Cpf)
            .HasConversion<EncryptedStringConverter>()
            .IsRequired();

        builder.Property(x => x.Rg)
            .HasConversion<EncryptedStringConverter>();

        builder.Property(x => x.Email)
            .HasConversion<EncryptedStringConverter>()
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasConversion<EncryptedStringConverter>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();

        builder.OwnsOne(x => x.HomeAddress, a =>
        {
            a.Property(p => p.Street).HasColumnName("home_street");
            a.Property(p => p.Number).HasColumnName("home_number");
            a.Property(p => p.Complement).HasColumnName("home_complement");
            a.Property(p => p.District).HasColumnName("home_district");
            a.Property(p => p.City).HasColumnName("home_city");
            a.Property(p => p.State).HasColumnName("home_state");
            a.Property(p => p.ZipCode).HasColumnName("home_zip");
        });

        // Global soft-delete filter is handled by AppDbContext for ISoftDeletable entities
        // builder.HasQueryFilter(x => x.DeletedAt == null);
        
        builder.Ignore(x => x.DomainEvents);
    }
}
