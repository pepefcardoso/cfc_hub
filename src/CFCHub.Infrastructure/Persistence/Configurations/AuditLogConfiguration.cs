using CFCHub.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ChangedFields)
            .HasColumnType("jsonb");
            
        builder.Property(e => e.EntityType)
            .HasMaxLength(200);
            
        builder.Property(e => e.EntityId)
            .HasMaxLength(200);
            
        builder.Property(e => e.Action)
            .HasMaxLength(50);
            
        builder.Property(e => e.ActorRole)
            .HasMaxLength(100);
            
        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);
            
        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(e => e.TraceId)
            .HasMaxLength(100);
    }
}
