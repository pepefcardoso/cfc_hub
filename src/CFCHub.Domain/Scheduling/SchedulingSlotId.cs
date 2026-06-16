using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public sealed record SchedulingSlotId(Guid Value) : StronglyTypedId<Guid>(Value);
