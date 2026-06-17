using System;
using System.Linq;
using System.Threading.Tasks;
using CFCHub.IntegrationTests.Builders;
using CFCHub.IntegrationTests.Common;
using CFCHub.Domain.Enrollment;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CFCHub.IntegrationTests.Lgpd;

[Collection("IntegrationTests")]
public class AuditLogTests : IntegrationTestBase
{
    public AuditLogTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task UpdateStudent_WritesAuditLog()
    {
        // Arrange
        var command = new StudentIntegrationBuilder().BuildCommand();
        var result = await Sender.Send(command);
        var studentId = new StudentId(result.StudentId);

        var student = await DbContext.Set<Student>().FirstOrDefaultAsync(s => s.Id == studentId);
        student.Should().NotBeNull();

        // Act
        DbContext.Entry(student!).Property(x => x.Name).CurrentValue = "Updated Name";
        await DbContext.SaveChangesAsync();

        // Assert
        var auditLogs = await DbContext.Set<CFCHub.Infrastructure.Auditing.AuditLog>()
            .Where(a => a.EntityId == studentId.Value.ToString() && a.Action == "Modified")
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
        var log = auditLogs.First();
        log.EntityType.Should().Be("Student");
    }

    [Fact]
    public async Task AuditLog_ContainsNoPlaintextPii()
    {
        // Arrange
        var command = new StudentIntegrationBuilder().WithCpf("99988877766").BuildCommand();
        var result = await Sender.Send(command);
        var studentId = new StudentId(result.StudentId);

        var student = await DbContext.Set<Student>().FirstOrDefaultAsync(s => s.Id == studentId);

        // Act
        DbContext.Entry(student!).Property(x => x.Name).CurrentValue = "Another Updated Name";
        await DbContext.SaveChangesAsync();

        // Assert
        var auditLogs = await DbContext.Set<CFCHub.Infrastructure.Auditing.AuditLog>()
            .Where(a => a.EntityId == studentId.Value.ToString())
            .ToListAsync();

        auditLogs.Should().NotBeEmpty();

        foreach (var log in auditLogs)
        {
            var changedFields = log.ChangedFields;
            // CPF and Name are marked with [Sensitive], so they should be masked
            changedFields.Should().NotContain("99988877766"); // raw CPF
            changedFields.Should().Contain("[encrypted]");
        }
    }

    [Fact]
    public async Task AuditLog_RowLevelSecurity_CannotUpdate()
    {
        // Arrange
        var command = new StudentIntegrationBuilder().BuildCommand();
        var result = await Sender.Send(command);
        var studentId = result.StudentId.ToString();

        // Ensure we have an audit log
#pragma warning disable EF1002
        var count = await DbContext.Database.ExecuteSqlRawAsync(
            $"SELECT 1 FROM {TenantSchema}.audit_logs WHERE entity_id = '{studentId}' LIMIT 1");

        // Act & Assert
        Func<Task> actUpdate = async () =>
        {
            await DbContext.Database.ExecuteSqlRawAsync(
                $"UPDATE {TenantSchema}.audit_logs SET action = 'tampered' WHERE entity_id = '{studentId}'");
        };
#pragma warning restore EF1002

        // RLS will reject this UPDATE, throwing a Postgres exception or DbUpdateException
        await actUpdate.Should().ThrowAsync<Exception>();
    }
}
