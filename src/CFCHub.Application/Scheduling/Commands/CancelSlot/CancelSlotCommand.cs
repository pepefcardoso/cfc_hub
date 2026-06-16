using System;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.CancelSlot;

public record CancelSlotCommand(Guid SlotId, string Reason) : IRequest;
