using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Compliance;

public sealed record DocumentRecordId(Guid Value) : StronglyTypedId<Guid>(Value);
