using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Compliance;

public sealed record DocumentExpiryAlertId(Guid Value) : StronglyTypedId<Guid>(Value);
