using System;
using MediatR;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Application.Enrollment.Commands.RequestDataErasure;

public record RequestDataErasureResult(DataErasureRequestStatus Status, string? BlockReason);

public record RequestDataErasureCommand(Guid StudentId) : IRequest<RequestDataErasureResult>;
