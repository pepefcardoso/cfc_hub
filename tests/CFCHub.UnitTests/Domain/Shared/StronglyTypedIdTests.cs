using System;
using CFCHub.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Shared;

public class StronglyTypedIdTests
{
    [Fact]
    public void StronglyTypedId_ImplicitConversion_ReturnsUnderlyingValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new StronglyTypedId<Guid>(guid);

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
        var id = new StronglyTypedId<Guid>(guid);

        // Act
        var result = id.ToString();

        // Assert
        result.Should().Be(guid.ToString());
    }
}
