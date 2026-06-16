using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Finance.Events;

public sealed record PaymentReceivedEvent(PaymentId PaymentId, DateTimeOffset OccurredAt) : IDomainEvent;
