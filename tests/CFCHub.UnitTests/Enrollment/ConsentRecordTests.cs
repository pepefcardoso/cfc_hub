using System;
using System.Linq;
using System.Reflection;
using CFCHub.Domain.Enrollment;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Enrollment;

public class ConsentRecordTests
{
    [Fact]
    public void ConsentRecord_IsImmutable_HasNoUpdateMethod()
    {
        var methods = typeof(ConsentRecord).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        
        var updateMethods = methods.Where(m => m.Name.StartsWith("Update") || m.Name.StartsWith("Set")).ToList();

        updateMethods.Should().BeEmpty("ConsentRecord should be immutable and have no update methods.");
    }

    [Fact]
    public void ConsentRecord_Capture_ReturnsValidRecord()
    {
        var id = new ConsentRecordId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        var record = ConsentRecord.Capture(id, studentId, "1.0", "hash", now, "127.0.0.1", "Browser", ConsentChannel.Web);

        record.Id.Should().Be(id);
        record.StudentId.Should().Be(studentId);
        record.PolicyVersion.Should().Be("1.0");
        record.PolicyContentHash.Should().Be("hash");
        record.ConsentedAt.Should().Be(now);
        record.IpAddress.Should().Be("127.0.0.1");
        record.UserAgent.Should().Be("Browser");
        record.Channel.Should().Be(ConsentChannel.Web);
    }
}
