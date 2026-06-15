using System;
using CFCHub.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared;

public class EntityTests
{
    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id)
        {
        }
    }

    private class TestDomainEvent : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }

    [Fact]
    public void Entity_WithSameId_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act
        var areEqual = entity1.Equals(entity2);
        var opEqual = entity1 == entity2;

        // Assert
        areEqual.Should().BeTrue();
        opEqual.Should().BeTrue();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Entity_WithDifferentId_AreNotEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act
        var areEqual = entity1.Equals(entity2);
        var opEqual = entity1 == entity2;

        // Assert
        areEqual.Should().BeFalse();
        opEqual.Should().BeFalse();
    }

    [Fact]
    public void AddDomainEvent_AppendsToList()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();

        // Act
        entity.AddDomainEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().ContainSingle();
        entity.DomainEvents[0].Should().Be(domainEvent);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesList()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());
        entity.AddDomainEvent(new TestDomainEvent());

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }
}
