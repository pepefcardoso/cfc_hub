using System;
using CFCHub.Domain.Shared;
using CFCHub.Application.Common.Pagination;
using CFCHub.Application.Scheduling.Queries.GetSlotsByInstructor;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetSlotsByStudent;

public record GetSlotsByStudentQuery(
    Guid StudentId,
    string? Cursor,
    int Limit = 20) : IRequest<PagedResult<SlotResult>>;
