using System;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.MarkNoShow;

public record MarkNoShowCommand(Guid SlotId) : IRequest;
