using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public record DataErasureRequestId(Guid Value) : StronglyTypedId<Guid>(Value);
