using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Finance;

public sealed record InstallmentId(Guid Value) : StronglyTypedId<Guid>(Value);
