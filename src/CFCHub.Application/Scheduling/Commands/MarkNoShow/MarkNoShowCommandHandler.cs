using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Scheduling.Commands.MarkNoShow;

public class MarkNoShowCommandHandler : IRequestHandler<MarkNoShowCommand>
{
    private readonly ISchedulingRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNoShowCommandHandler(
        ISchedulingRepository repository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkNoShowCommand request, CancellationToken cancellationToken)
    {
        var slotId = new SchedulingSlotId(request.SlotId);
        var slot = await _repository.GetSlotByIdAsync(slotId, cancellationToken);

        if (slot == null)
        {
            throw new NotFoundException($"Slot {request.SlotId} não encontrado.");
        }

        if (_currentUserService.Role != RoleType.Admin && _currentUserService.Role != RoleType.Instructor)
        {
            throw new ForbiddenException("Apenas instrutores ou administradores podem registrar ausência (no-show).", "FORBIDDEN");
        }

        slot.MarkNoShow();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
