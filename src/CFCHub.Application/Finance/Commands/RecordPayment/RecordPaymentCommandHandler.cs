using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Finance.Commands.RecordPayment;

public class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Guid>
{
    private readonly IInstallmentRepository _installmentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;

    public RecordPaymentCommandHandler(
        IInstallmentRepository installmentRepository,
        IPaymentRepository paymentRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork,
        ISystemClock clock,
        IIdGenerator idGenerator)
    {
        _installmentRepository = installmentRepository;
        _paymentRepository = paymentRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<Guid> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        var installmentId = new InstallmentId(request.InstallmentId);
        var installment = await _installmentRepository.GetByIdAsync(installmentId, cancellationToken);
        if (installment == null)
        {
            throw new NotFoundException($"Installment with id {request.InstallmentId} not found.");
        }

        if (installment.Status == InstallmentStatus.Paid)
        {
            throw new ConflictException("Installment is already paid.");
        }

        if (installment.EnrollmentId.Value != request.EnrollmentId)
        {
            throw new UnprocessableException("Installment does not belong to the specified enrollment.");
        }

        if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
        {
            throw new UnprocessableException($"Invalid payment method: {request.Method}");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var paymentId = _idGenerator.NewId<PaymentId>();
            var payment = Payment.Create(
                paymentId,
                new StudentId(request.StudentId),
                new EnrollmentId(request.EnrollmentId),
                new Money(request.Amount, request.Currency),
                method);

            payment.Confirm(_clock);
            installment.MarkPaid(paymentId);

            await _paymentRepository.AddAsync(payment, cancellationToken);
            
            var payload = JsonSerializer.Serialize(new { PaymentId = payment.Id.Value });
            await _outboxService.InsertAsync("PaymentReceiptRequested", payload, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return payment.Id.Value;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
