using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Enrollment.Commands.EnrollStudent;

public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, Guid>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;

    public EnrollStudentCommandHandler(
        IStudentRepository studentRepository,
        IEnrollmentRepository enrollmentRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork,
        ISystemClock clock,
        IIdGenerator idGenerator)
    {
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<Guid> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);
        
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            throw new NotFoundException($"Student with id {request.StudentId} not found.");
        }

        if (student.Status != StudentStatus.Active)
        {
            throw new ConflictException("Student is not active.");
        }

        var existingEnrollments = await _enrollmentRepository.GetByStudentIdAsync(studentId, cancellationToken);
        if (existingEnrollments.Any(e => e.Category == request.Category && e.Status == EnrollmentStatus.Active))
        {
            throw new ConflictException("ENROLLMENT_ALREADY_EXISTS");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var enrollmentId = _idGenerator.NewId<EnrollmentId>();
            var enrollment = Domain.Enrollment.Enrollment.Enroll(enrollmentId, studentId, request.Category, _clock);

            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            
            var contractGenerationPayload = JsonSerializer.Serialize(new { ContractId = Guid.NewGuid(), StudentId = request.StudentId, Category = request.Category });
            await _outboxService.InsertAsync("ContractGenerationRequested", contractGenerationPayload, cancellationToken);
            
            var paymentPlanPayload = JsonSerializer.Serialize(new { StudentId = request.StudentId, Category = request.Category });
            await _outboxService.InsertAsync("PaymentPlanCreationRequested", paymentPlanPayload, cancellationToken);
            
            var documentTrackingPayload = JsonSerializer.Serialize(new { StudentId = request.StudentId, Category = request.Category });
            await _outboxService.InsertAsync("DocumentTrackingRegistered", documentTrackingPayload, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return enrollment.Id.Value;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
