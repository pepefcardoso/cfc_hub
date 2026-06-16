using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Contracts;

public record SignatureRecordId(Guid Value) : StronglyTypedId<Guid>(Value);
