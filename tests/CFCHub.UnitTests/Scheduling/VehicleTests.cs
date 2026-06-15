using System;
using CFCHub.Domain.Scheduling;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Scheduling;

public class VehicleTests
{
    [Fact]
    public void IsAvailableAt_WhenInMaintenance_ReturnsFalse()
    {
        // Arrange
        var time = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(-3));
        var vehicle = new Builders.VehicleBuilder()
            .WithMaintenanceUntil(time.AddHours(2))
            .Build();

        // Act
        var result = vehicle.IsAvailableAt(time);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAvailableAt_WhenMaintenanceHasPassed_ReturnsTrue()
    {
        // Arrange
        var time = new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.FromHours(-3));
        var vehicle = new Builders.VehicleBuilder()
            .WithMaintenanceUntil(time.AddHours(-2))
            .Build();

        // Act
        var result = vehicle.IsAvailableAt(time);

        // Assert
        result.Should().BeTrue();
    }
}
