using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Scheduling;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new VehicleId(value));

        builder.Property(x => x.LicensePlate)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(x => x.MaintenanceUntil);

        builder.Ignore(x => x.DomainEvents);
    }
}
