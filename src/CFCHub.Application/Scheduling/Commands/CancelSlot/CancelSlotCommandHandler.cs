using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.CancelSlot;

public class CancelSlotCommandHandler : IRequestHandler<CancelSlotCommand>
{
    private readonly ISchedulingRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;

    public CancelSlotCommandHandler(
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

    public async Task Handle(CancelSlotCommand request, CancellationToken cancellationToken)
    {
        var slotId = new SchedulingSlotId(request.SlotId);
        var slot = await _repository.GetSlotByIdAsync(slotId, cancellationToken);

        if (slot == null)
        {
            throw new NotFoundException($"Slot {request.SlotId} não encontrado.");
        }

        var isOwner = _currentUserService.UserId == slot.StudentId.Value;
        var hasRole = _currentUserService.Role is RoleType.Admin or RoleType.Receptionist or RoleType.Instructor;

        if (!isOwner && !hasRole)
        {
            throw new ForbiddenException("Você não tem permissão para cancelar este agendamento.", "FORBIDDEN");
        }

        slot.Cancel(request.Reason, _clock);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
