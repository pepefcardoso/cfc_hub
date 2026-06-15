using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public class Vehicle : AggregateRoot<VehicleId>
{
    public string LicensePlate { get; private set; }
    public CnhCategory Category { get; private set; }
    public VehicleStatus Status { get; private set; }
    public DateTimeOffset? MaintenanceUntil { get; private set; }

    private Vehicle() 
    { 
        LicensePlate = null!;
    }

    public Vehicle(VehicleId id, string licensePlate, CnhCategory category) : base(id)
    {
        LicensePlate = licensePlate;
        Category = category;
        Status = VehicleStatus.Active;
    }

    public void SetMaintenance(DateTimeOffset until)
    {
        Status = VehicleStatus.InMaintenance;
        MaintenanceUntil = until;
    }

    public void SetStatus(VehicleStatus status)
    {
        Status = status;
    }

    public bool IsAvailableAt(DateTimeOffset time)
    {
        if (Status == VehicleStatus.Retired) return false;
        if (Status == VehicleStatus.InMaintenance && MaintenanceUntil.HasValue && MaintenanceUntil.Value > time)
            return false;

        return true;
    }
}
