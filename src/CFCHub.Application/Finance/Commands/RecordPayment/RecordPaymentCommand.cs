using System;
using MediatR;

namespace CFCHub.Application.Finance.Commands.RecordPayment;

public record RecordPaymentCommand(
    Guid StudentId,
    Guid EnrollmentId,
    Guid InstallmentId,
    decimal Amount,
    string Currency,
    string Method) : IRequest<Guid>;
