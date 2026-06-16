using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Finance;

public sealed record PaymentId(Guid Value) : StronglyTypedId<Guid>(Value);
