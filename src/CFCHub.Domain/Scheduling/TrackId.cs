using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public sealed record TrackId(Guid Value) : StronglyTypedId<Guid>(Value);
