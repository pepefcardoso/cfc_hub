using CFCHub.Domain.Shared;
using MediatR;
using System;

namespace CFCHub.Application.Enrollment.Queries.GetEnrollments;

public record EnrollmentResult(Guid Id, Guid StudentId, string Category, string Status, DateOnly CreatedAt);

public record GetEnrollmentsQuery(Guid StudentId, int Limit, string? Cursor) : IRequest<PagedResult<EnrollmentResult>>;
