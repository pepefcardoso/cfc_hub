using System;
using MediatR;

namespace CFCHub.Application.Enrollment.Queries.GetStudent;

public record GetStudentQuery(Guid StudentId) : IRequest<StudentResult>;
