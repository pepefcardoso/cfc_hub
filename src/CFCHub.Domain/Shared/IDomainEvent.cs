using System;
using MediatR;

namespace CFCHub.Domain.Shared;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}
