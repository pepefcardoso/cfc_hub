using System;
using CFCHub.Application.Common.Models;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetAvailableSlots;

public record GetAvailableSlotsQuery(
    DateOnly Date,
    CnhCategory? Category,
    InstructorId? InstructorId,
    string? Cursor,
    int Limit = 20
) : IRequest<PagedResult<AvailableSlotResult>>;
