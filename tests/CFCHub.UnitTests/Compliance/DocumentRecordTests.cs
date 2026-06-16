using System;
using System.Linq;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Compliance.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Compliance;

public class DocumentRecordTests
{
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly StudentId _studentId;

    public DocumentRecordTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _idGenerator = Substitute.For<IIdGenerator>();
        _idGenerator.NewId<DocumentRecordId>().Returns(new DocumentRecordId(Guid.NewGuid()));
        _studentId = new StudentId(Guid.NewGuid());
    }

    [Fact]
    public void MarkAlertSent_WhenSentToday_ThrowsUnprocessable()
    {
        // Arrange
        var record = DocumentRecord.Create(_studentId, DocumentType.MedicalExam, DateOnly.FromDateTime(DateTime.Today.AddDays(30)), _idGenerator);
        var now = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(now);
        
        record.MarkAlertSent(AlertTier.D30, _clock);

        // Advance clock by 12 hours (less than 24h)
        _clock.UtcNow.Returns(now.AddHours(12));

        // Act
        var act = () => record.MarkAlertSent(AlertTier.D15, _clock);

        // Assert
        act.Should().Throw<UnprocessableException>().WithMessage("ALERT_ALREADY_SENT_TODAY");
    }

    [Fact]
    public void MarkAlertSent_RaisesEvent()
    {
        // Arrange
        var record = DocumentRecord.Create(_studentId, DocumentType.MedicalExam, DateOnly.FromDateTime(DateTime.Today.AddDays(30)), _idGenerator);
        var now = DateTimeOffset.UtcNow;
        _clock.UtcNow.Returns(now);

        // Act
        record.MarkAlertSent(AlertTier.D30, _clock);

        // Assert
        record.DomainEvents.Should().ContainSingle(e => e is DocumentExpiryAlertRequestedEvent);
        var domainEvent = (DocumentExpiryAlertRequestedEvent)record.DomainEvents.Single();
        
        domainEvent.DocumentRecordId.Should().Be(record.Id);
        domainEvent.StudentId.Should().Be(_studentId.Value);
        domainEvent.DocumentType.Should().Be(DocumentType.MedicalExam);
        domainEvent.Tier.Should().Be(AlertTier.D30);
        domainEvent.RequestedAt.Should().Be(now);
        record.LastAlertSentAt.Should().Be(now);
    }
}
