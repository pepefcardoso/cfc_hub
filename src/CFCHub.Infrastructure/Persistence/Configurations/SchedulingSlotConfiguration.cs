using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Students;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class SchedulingSlotConfiguration : IEntityTypeConfiguration<SchedulingSlot>
{
    public void Configure(EntityTypeBuilder<SchedulingSlot> builder)
    {
        builder.ToTable("scheduling_slots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SchedulingSlotId(value));

        builder.Property(x => x.InstructorId)
            .HasConversion(id => id.Value, value => new InstructorId(value))
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .HasConversion(id => id.Value, value => new VehicleId(value))
            .IsRequired();

        builder.Property(x => x.TrackId)
            .HasConversion(id => id.Value, value => new TrackId(value))
            .IsRequired();

        builder.Property(x => x.StudentId)
            .HasConversion(id => id.Value, value => new StudentId(value))
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.EndedAt)
            .IsRequired();

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        builder.Ignore(x => x.DomainEvents);

        // Exclusion constraints added via migrationBuilder.Sql(...) in migration
        // because EF Core doesn't support gist natively via Fluent API
    }
}
