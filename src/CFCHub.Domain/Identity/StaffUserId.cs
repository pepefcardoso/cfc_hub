using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Identity;

public sealed record StaffUserId(Guid Value) : StronglyTypedId<Guid>(Value);
