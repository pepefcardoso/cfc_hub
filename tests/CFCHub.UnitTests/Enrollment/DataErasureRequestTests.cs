using System;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Enrollment;

public class DataErasureRequestTests
{
    private readonly ISystemClock _clock;

    public DataErasureRequestTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DataErasureRequest_Create_ReturnsPendingRequest()
    {
        var id = new DataErasureRequestId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());

        var req = DataErasureRequest.Create(id, studentId, _clock);

        req.Id.Should().Be(id);
        req.StudentId.Should().Be(studentId);
        req.Status.Should().Be(DataErasureRequestStatus.Pending);
    }

    [Fact]
    public void DataErasureRequest_Block_SetsStatusAndReason()
    {
        var req = DataErasureRequest.Create(new DataErasureRequestId(Guid.NewGuid()), new StudentId(Guid.NewGuid()), _clock);

        req.Block("Reason");

        req.Status.Should().Be(DataErasureRequestStatus.Blocked);
        req.BlockReason.Should().Be("Reason");
    }

    [Fact]
    public void DataErasureRequest_Complete_SetsStatusAndCompletedAt()
    {
        var req = DataErasureRequest.Create(new DataErasureRequestId(Guid.NewGuid()), new StudentId(Guid.NewGuid()), _clock);

        req.Complete(_clock);

        req.Status.Should().Be(DataErasureRequestStatus.Completed);
        req.CompletedAt.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void DataErasureRequest_Properties_SetGet()
    {
        var req = DataErasureRequest.Create(new DataErasureRequestId(Guid.NewGuid()), new StudentId(Guid.NewGuid()), _clock);
        
        req.CreatedAt = _clock.UtcNow;
        req.CreatedBy = "user1";
        req.UpdatedAt = _clock.UtcNow;
        req.UpdatedBy = "user2";

        req.CreatedAt.Should().Be(_clock.UtcNow);
        req.CreatedBy.Should().Be("user1");
        req.UpdatedAt.Should().Be(_clock.UtcNow);
        req.UpdatedBy.Should().Be("user2");
    }

    [Fact]
    public void DataErasureRequest_EfCoreConstructor_Exists()
    {
        var instance = Activator.CreateInstance(typeof(DataErasureRequest), true);
        instance.Should().NotBeNull();
    }
}
