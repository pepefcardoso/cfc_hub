using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public sealed record VehicleId(Guid Value) : StronglyTypedId<Guid>(Value);
