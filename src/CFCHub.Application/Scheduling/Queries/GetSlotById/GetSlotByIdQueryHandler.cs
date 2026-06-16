using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetSlotById;

public class GetSlotByIdQueryHandler : IRequestHandler<GetSlotByIdQuery, Result<SlotDetailsResult>>
{
    private readonly ISchedulingRepository _repository;

    public GetSlotByIdQueryHandler(ISchedulingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SlotDetailsResult>> Handle(GetSlotByIdQuery request, CancellationToken cancellationToken)
    {
        var slotId = new SchedulingSlotId(request.SlotId);
        var slot = await _repository.GetSlotByIdAsync(slotId, cancellationToken);

        if (slot == null)
            return Result<SlotDetailsResult>.Failure(Error.NotFound("SchedulingSlot.NotFound", "Slot not found."));

        return Result<SlotDetailsResult>.Success(new SlotDetailsResult(
            slot.Id.Value,
            slot.InstructorId.Value,
            slot.VehicleId.Value,
            slot.TrackId.Value,
            slot.StudentId.Value,
            slot.Category,
            slot.StartedAt,
            slot.EndedAt,
            slot.Status,
            slot.CancellationReason
        ));
    }
}
