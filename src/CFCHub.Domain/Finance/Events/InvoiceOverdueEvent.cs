using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Finance.Events;

public sealed record InvoiceOverdueEvent(InstallmentId InstallmentId, DateTimeOffset OccurredAt) : IDomainEvent;
