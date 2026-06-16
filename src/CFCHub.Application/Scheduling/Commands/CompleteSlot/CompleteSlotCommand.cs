using System;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.CompleteSlot;

public record CompleteSlotCommand(Guid SlotId) : IRequest;
