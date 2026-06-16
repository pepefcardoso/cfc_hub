using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Identity;
using System;
using System.Linq;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.ToTable("instructors");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new InstructorId(value));

        builder.Property(x => x.LinkedUserId)
            .HasConversion(id => id.Value, value => new StaffUserId(value))
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        var teachableCategoriesComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<System.Collections.Generic.IReadOnlyList<CnhCategory>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList().AsReadOnly());

        builder.Property(x => x.TeachableCategories)
            .HasConversion(
                v => v.Select(c => c.ToString()).ToArray(),
                v => v.Select(s => Enum.Parse<CnhCategory>(s)).ToList())
            .Metadata.SetValueComparer(teachableCategoriesComparer);
            
        builder.Property(x => x.TeachableCategories).HasColumnType("TEXT[]");

        builder.Property(x => x.MaxDailySlots)
            .IsRequired();

        builder.OwnsOne(x => x.WeeklyTemplate, InstructorAvailabilityTemplateConfiguration.Configure);

        builder.OwnsMany(x => x.Overrides, ob => 
        {
            ob.ToJson();
        });

        builder.Ignore(x => x.DomainEvents);
    }
}
