using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public sealed record InstructorId(Guid Value) : StronglyTypedId<Guid>(Value);
