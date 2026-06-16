using System;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.BookSlot;

public record BookSlotCommand(
    Guid InstructorId,
    Guid VehicleId,
    Guid TrackId,
    Guid StudentId,
    CnhCategory Category,
    DateTimeOffset StartedAt) : IRequest<Result<BookSlotResult>>;
