using System;
using CFCHub.Domain.Scheduling;

namespace CFCHub.UnitTests.Builders;

public class VehicleBuilder
{
    private VehicleId _id = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private string _licensePlate = "ABC-1234";
    private CnhCategory _category = CnhCategory.B;
    private DateTimeOffset? _maintenanceUntil;

    public VehicleBuilder WithId(Guid id)
    {
        _id = new VehicleId(id);
        return this;
    }

    public VehicleBuilder WithMaintenanceUntil(DateTimeOffset time)
    {
        _maintenanceUntil = time;
        return this;
    }

    public Vehicle Build()
    {
        var vehicle = new Vehicle(_id, _licensePlate, _category);
        if (_maintenanceUntil.HasValue)
        {
            vehicle.SetMaintenance(_maintenanceUntil.Value);
        }
        return vehicle;
    }
}
