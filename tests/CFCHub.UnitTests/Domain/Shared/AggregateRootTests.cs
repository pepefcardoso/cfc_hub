using System;
using CFCHub.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared;

public class AggregateRootTests
{
    private class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot(Guid id) : base(id)
        {
        }
    }

    private class TestDomainEvent : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }

    [Fact]
    public void AggregateRoot_AddDomainEvent_AppendsToInternalList()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();

        // Act
        aggregate.AddDomainEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().ContainSingle();
        aggregate.DomainEvents[0].Should().Be(domainEvent);
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_EmptiesList()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.AddDomainEvent(new TestDomainEvent());

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }
}
