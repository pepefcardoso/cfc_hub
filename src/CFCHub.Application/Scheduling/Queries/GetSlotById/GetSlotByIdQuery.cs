using System;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetSlotById;

public record GetSlotByIdQuery(Guid SlotId) : IRequest<Result<SlotDetailsResult>>;
