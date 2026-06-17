using System;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Scheduling;

public class VehicleTests
{
    [Fact]
    public void Vehicle_IsAvailableAt_WhenInMaintenance_ReturnsFalse()
    {
        // Arrange
        var checkTime = new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero);
        var maintenanceUntil = checkTime.AddDays(1);

        var vehicle = new VehicleBuilder()
            .WithMaintenanceUntil(maintenanceUntil)
            .Build();

        // Act
        var isAvailable = vehicle.IsAvailableAt(checkTime);

        // Assert
        isAvailable.Should().BeFalse();
    }
}
