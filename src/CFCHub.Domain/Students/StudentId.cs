using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Students;

public sealed record StudentId(Guid Value) : StronglyTypedId<Guid>(Value);
