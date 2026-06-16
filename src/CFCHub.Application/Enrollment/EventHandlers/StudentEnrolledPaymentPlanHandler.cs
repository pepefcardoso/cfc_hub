using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Enrollment.EventHandlers;

public class StudentEnrolledPaymentPlanHandler : INotificationHandler<StudentEnrolledEvent>
{
    private readonly IInstallmentRepository _installmentRepository;
    private readonly IIdGenerator _idGenerator;

    public StudentEnrolledPaymentPlanHandler(
        IInstallmentRepository installmentRepository,
        IIdGenerator idGenerator)
    {
        _installmentRepository = installmentRepository;
        _idGenerator = idGenerator;
    }

    public async Task Handle(StudentEnrolledEvent notification, CancellationToken cancellationToken)
    {
        var installments = new List<Installment>();
        var totalAmount = 2400m; // Mock total amount for demonstration
        var amountPerInstallment = totalAmount / 12m;

        for (int i = 0; i < 12; i++)
        {
            var installmentId = _idGenerator.NewId<InstallmentId>();
            var installment = Installment.Create(
                installmentId,
                notification.EnrollmentId,
                new Money(amountPerInstallment, "BRL"),
                System.DateOnly.FromDateTime(notification.OccurredOn.AddMonths(i).DateTime)
            );
            installments.Add(installment);
        }

        await _installmentRepository.AddRangeAsync(installments, cancellationToken);
    }
}
