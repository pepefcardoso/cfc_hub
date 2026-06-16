using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Enrollment.Commands.CreateStudent;

public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, CreateStudentResult>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IConsentRecordRepository _consentRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUserService _currentUserService;

    public CreateStudentCommandHandler(
        IStudentRepository studentRepository,
        IConsentRecordRepository consentRecordRepository,
        IUnitOfWork unitOfWork,
        ISystemClock clock,
        IIdGenerator idGenerator,
        ICurrentUserService currentUserService)
    {
        _studentRepository = studentRepository;
        _consentRecordRepository = consentRecordRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
        _currentUserService = currentUserService;
    }

    public async Task<CreateStudentResult> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        // Hash CPF to search since it's anonymized or stored encrypted if applicable,
        // although IStudentRepository.GetByCpfAsync likely expects the hashed CPF for lookups 
        // per architectural mandate of data minimization.
        string hashedCpf = HashCpf(request.Cpf);
        var existingStudent = await _studentRepository.GetByCpfAsync(hashedCpf, cancellationToken);
        if (existingStudent != null)
        {
            throw new ConflictException("STUDENT_ALREADY_EXISTS");
        }

        var studentId = _idGenerator.NewId<StudentId>();
        var homeAddress = new Address(
            request.HomeAddress.Street,
            request.HomeAddress.Number,
            request.HomeAddress.Complement,
            request.HomeAddress.District,
            request.HomeAddress.City,
            request.HomeAddress.State,
            request.HomeAddress.ZipCode);

        var student = Student.Create(
            studentId,
            request.Name,
            request.Cpf,
            request.Rg,
            request.Email,
            request.Phone,
            request.BirthDate,
            homeAddress,
            _clock,
            _idGenerator);

        var consentRecordId = _idGenerator.NewId<ConsentRecordId>();
        var consentRecord = ConsentRecord.Capture(
            consentRecordId,
            student.Id,
            request.PolicyVersion,
            request.PolicyContentHash,
            _clock.UtcNow,
            _currentUserService.IpAddress,
            _currentUserService.UserAgent ?? "Unknown",
            request.ConsentChannel);

        student.AddDomainEvent(new StudentWelcomeEmailRequestedEvent(student.Id, _clock.UtcNow));

        await _studentRepository.AddAsync(student, cancellationToken);
        await _consentRecordRepository.AddAsync(consentRecord, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateStudentResult(student.Id.Value);
    }

    private static string HashCpf(string cpf)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(cpf));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
