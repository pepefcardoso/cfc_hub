using System;
using CFCHub.Domain.Scheduling;
using MediatR;

namespace CFCHub.Application.Enrollment.Commands.EnrollStudent;

public record EnrollStudentCommand(Guid StudentId, CnhCategory Category) : IRequest<Guid>;
