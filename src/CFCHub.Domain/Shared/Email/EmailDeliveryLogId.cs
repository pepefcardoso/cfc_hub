using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Shared.Email;

public record EmailDeliveryLogId(Guid Value) : StronglyTypedId<Guid>(Value);
