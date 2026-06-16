using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Finance;

public sealed record InvoiceId(Guid Value) : StronglyTypedId<Guid>(Value);
