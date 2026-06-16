using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Enrollment.Queries.GetStudents;

public record GetStudentsQuery(int PageSize, string? Cursor) : IRequest<PagedResult<GetStudent.StudentResult>>;
