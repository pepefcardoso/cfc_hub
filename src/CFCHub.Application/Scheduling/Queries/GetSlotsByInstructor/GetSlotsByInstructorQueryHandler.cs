using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Pagination;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Scheduling;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetSlotsByInstructor;

public class GetSlotsByInstructorQueryHandler : IRequestHandler<GetSlotsByInstructorQuery, PagedResult<SlotResult>>
{
    private readonly ISchedulingRepository _repository;

    public GetSlotsByInstructorQueryHandler(ISchedulingRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<SlotResult>> Handle(GetSlotsByInstructorQuery request, CancellationToken cancellationToken)
    {
        var cursor = request.Cursor;
        var instructorId = new InstructorId(request.InstructorId);
        
        var pagedProjections = await _repository.GetByInstructorAsync(instructorId, cursor, request.Limit, cancellationToken);

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
