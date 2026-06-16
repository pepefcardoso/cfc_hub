using CFCHub.Domain.Identity;
using CFCHub.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class StaffUserConfiguration : IEntityTypeConfiguration<StaffUser>
{
    public void Configure(EntityTypeBuilder<StaffUser> builder)
    {
        builder.ToTable("staff_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new StaffUserId(value))
            .IsRequired();

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .HasConversion<EncryptedStringConverter>()
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasColumnType("TEXT")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("status")
            .HasColumnType("TEXT")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Ignore(u => u.DomainEvents);
    }
}
