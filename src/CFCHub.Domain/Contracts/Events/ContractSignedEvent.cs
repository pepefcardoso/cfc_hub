using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Contracts.Events;

public record ContractSignedEvent(ContractId ContractId, DateTimeOffset OccurredAt) : IDomainEvent;
