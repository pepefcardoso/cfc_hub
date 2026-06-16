using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Contracts.Events;

public record ContractGenerationRequestedEvent(ContractId ContractId, DateTimeOffset OccurredAt) : IDomainEvent;
