using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Finance;
using CFCHub.Application.Common.Interfaces;

namespace CFCHub.Application.Enrollment.Commands.RequestDataErasure;

public class RequestDataErasureCommandHandler : IRequestHandler<RequestDataErasureCommand, RequestDataErasureResult>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IInstallmentRepository _installmentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IDataErasureRequestRepository _dataErasureRequestRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly ISystemClock _clock;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;

    public RequestDataErasureCommandHandler(
        IStudentRepository studentRepository,
        IEnrollmentRepository enrollmentRepository,
        IContractRepository contractRepository,
        IInstallmentRepository installmentRepository,
        IPaymentRepository paymentRepository,
        IDataErasureRequestRepository dataErasureRequestRepository,
        IIdGenerator idGenerator,
        ISystemClock clock,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _contractRepository = contractRepository;
        _installmentRepository = installmentRepository;
        _paymentRepository = paymentRepository;
        _dataErasureRequestRepository = dataErasureRequestRepository;
        _idGenerator = idGenerator;
        _clock = clock;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RequestDataErasureResult> Handle(RequestDataErasureCommand request, CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);

        if (student == null)
        {
            throw new NotFoundException($"Student with ID {request.StudentId} not found.");
        }

        var contract = await _contractRepository.GetByStudentIdAsync(studentId, cancellationToken);
        bool hasUnsignedContract = contract != null && 
                                   contract.Status != ContractStatus.Signed && 
                                   contract.Status != ContractStatus.Cancelled;

        var enrollments = await _enrollmentRepository.GetByStudentIdAsync(studentId, cancellationToken);
        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        
        bool hasUnpaidDebt = false;
        if (enrollmentIds.Any())
        {
            hasUnpaidDebt = await _installmentRepository.HasOverdueInstallmentsAsync(enrollmentIds, cancellationToken);
        }

        var payments = await _paymentRepository.GetByStudentIdAsync(studentId, cancellationToken);
        var fiveYearsAgo = _clock.UtcNow.AddYears(-5);
        bool hasRecentPayments = payments.Any(p => p.CreatedAt >= fiveYearsAgo);

        var erasureRequestId = _idGenerator.NewId<DataErasureRequestId>();
        var erasureRequest = DataErasureRequest.Create(erasureRequestId, studentId, _clock);

        if (hasUnsignedContract || hasUnpaidDebt)
        {
            if (hasUnsignedContract)
            {
                erasureRequest.Block("ACTIVE_CONTRACT");
            }
            else if (hasUnpaidDebt)
            {
                erasureRequest.Block("UNPAID_DEBT");
            }

            await _dataErasureRequestRepository.AddAsync(erasureRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new RequestDataErasureResult(erasureRequest.Status, erasureRequest.BlockReason);
        }

        // If no full block, proceed with pending erasure.
        // Partial block logic (hasRecentPayments) is handled downstream by the worker during data erasure.
        
        student.RequestErasure();
        await _studentRepository.UpdateAsync(student, cancellationToken);
        await _dataErasureRequestRepository.AddAsync(erasureRequest, cancellationToken);

        var payload = JsonSerializer.Serialize(new { StudentId = studentId.Value, RequestId = erasureRequestId.Value });
        await _outboxService.InsertAsync("DataErasureRequested", payload, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new RequestDataErasureResult(erasureRequest.Status, null);
    }
}
