using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.CompleteSlot;

public class CompleteSlotCommandHandler : IRequestHandler<CompleteSlotCommand>
{
    private readonly ISchedulingRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;

    public CompleteSlotCommandHandler(
        ISchedulingRepository repository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ISystemClock clock)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(CompleteSlotCommand request, CancellationToken cancellationToken)
    {
        var slotId = new SchedulingSlotId(request.SlotId);
        var slot = await _repository.GetSlotByIdAsync(slotId, cancellationToken);

        if (slot == null)
        {
            throw new NotFoundException($"Slot {request.SlotId} não encontrado.");
        }

        if (_currentUserService.Role != RoleType.Admin && _currentUserService.Role != RoleType.Instructor)
        {
            throw new ForbiddenException("Apenas instrutores ou administradores podem concluir um agendamento.", "FORBIDDEN");
        }

        slot.Complete(_clock);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
