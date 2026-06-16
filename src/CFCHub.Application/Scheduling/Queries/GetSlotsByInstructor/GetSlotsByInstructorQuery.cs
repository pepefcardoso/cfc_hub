using System;
using CFCHub.Domain.Shared;
using CFCHub.Application.Common.Pagination;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetSlotsByInstructor;

public record GetSlotsByInstructorQuery(
    Guid InstructorId,
    string? Cursor,
    int Limit = 20) : IRequest<PagedResult<SlotResult>>;
