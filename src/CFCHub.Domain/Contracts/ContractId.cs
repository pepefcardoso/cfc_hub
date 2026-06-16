using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Contracts;

public record ContractId(Guid Value) : StronglyTypedId<Guid>(Value);
