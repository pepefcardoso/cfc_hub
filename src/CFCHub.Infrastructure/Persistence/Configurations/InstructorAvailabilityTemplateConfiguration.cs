using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Scheduling;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public static class InstructorAvailabilityTemplateConfiguration
{
    public static void Configure(OwnedNavigationBuilder<Instructor, InstructorAvailabilityTemplate> builder)
    {
        builder.ToJson("weekly_template");
        builder.OwnsMany(x => x.Windows);
    }
}
