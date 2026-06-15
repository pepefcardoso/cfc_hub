using System;
using CFCHub.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared;

public class StronglyTypedIdTests
{
    private record TestId(Guid Value) : StronglyTypedId<Guid>(Value);

    [Fact]
    public void StronglyTypedId_ImplicitConversion_ReturnsUnderlyingValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new TestId(guid);

        // Act
        Guid result = id;

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void StronglyTypedId_ToString_ReturnsUnderlyingValueString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new TestId(guid);

        // Act
        var result = id.ToString();

        // Assert
        result.Should().Be(guid.ToString());
    }
}
