using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Enrollment.EventHandlers;

public class StudentEnrolledEventHandler : INotificationHandler<StudentEnrolledEvent>
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly ISystemClock _clock;

    public StudentEnrolledEventHandler(
        IContractRepository contractRepository,
        IIdGenerator idGenerator,
        ISystemClock clock)
    {
        _contractRepository = contractRepository;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task Handle(StudentEnrolledEvent notification, CancellationToken cancellationToken)
    {
        var contractId = _idGenerator.NewId<ContractId>();
        
        var contract = Contract.Create(
            contractId,
            notification.StudentId,
            notification.EnrollmentId,
            null, // templateKey resolved later
            _clock);

        await _contractRepository.AddAsync(contract, cancellationToken);
    }
}
