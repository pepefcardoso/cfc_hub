using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Students;

namespace CFCHub.Infrastructure.Persistence.Configurations;

public class DocumentRecordConfiguration : IEntityTypeConfiguration<DocumentRecord>
{
    public void Configure(EntityTypeBuilder<DocumentRecord> builder)
    {
        builder.ToTable("document_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DocumentRecordId(value));

        builder.Property(x => x.StudentId)
            .HasConversion(id => id.Value, value => new StudentId(value));

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasColumnType("TEXT");
    }
}
