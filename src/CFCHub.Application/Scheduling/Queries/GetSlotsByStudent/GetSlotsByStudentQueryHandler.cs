using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Pagination;
using CFCHub.Application.Scheduling.Queries.GetSlotsByInstructor;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetSlotsByStudent;

public class GetSlotsByStudentQueryHandler : IRequestHandler<GetSlotsByStudentQuery, PagedResult<SlotResult>>
{
    private readonly ISchedulingRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetSlotsByStudentQueryHandler(
        ISchedulingRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<SlotResult>> Handle(GetSlotsByStudentQuery request, CancellationToken cancellationToken)
    {
        var isOwner = _currentUserService.UserId == request.StudentId;
        var hasRole = _currentUserService.Role is RoleType.Admin or RoleType.Receptionist;

        if (!isOwner && !hasRole)
        {
            throw new ForbiddenException("Você não tem permissão para visualizar estes agendamentos.", "FORBIDDEN");
        }

        var cursor = request.Cursor;
        var studentId = new StudentId(request.StudentId);
        
        var pagedProjections = await _repository.GetByStudentAsync(studentId, cursor, request.Limit, cancellationToken);

        var resultItems = pagedProjections.Items.Select(p => new SlotResult(
            p.Id,
            p.Status,
            p.StartedAt,
            p.InstructorName,
            p.VehicleId,
            p.TrackType
        )).ToList();

        return new PagedResult<SlotResult>(
            resultItems,
            pagedProjections.NextCursor,
            pagedProjections.HasMore,
            pagedProjections.Count);
    }
}
