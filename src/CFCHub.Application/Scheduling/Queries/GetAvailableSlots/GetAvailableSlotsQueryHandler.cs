using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Scheduling.Queries.GetAvailableSlots;

public class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, PagedResult<AvailableSlotResult>>
{
    private readonly IAvailabilityCalculatorService _calculatorService;

    public GetAvailableSlotsQueryHandler(IAvailabilityCalculatorService calculatorService)
    {
        _calculatorService = calculatorService;
    }

    public async Task<PagedResult<AvailableSlotResult>> Handle(GetAvailableSlotsQuery request, CancellationToken ct)
    {
        var category = request.Category ?? CnhCategory.B; // Default to B if no category is provided
        
        var slotsResult = await _calculatorService.GetAvailableSlotsAsync(
            request.Date,
            category,
            request.InstructorId,
            request.Cursor,
            request.Limit,
            ct);

        var items = slotsResult.Items.Select(s => new AvailableSlotResult(
            s.StartedAt,
            s.InstructorId.Value,
            s.InstructorName,
            s.VehicleId.Value,
            s.TrackId.Value,
            s.TrackType
        )).ToList();

        return new PagedResult<AvailableSlotResult>(items, slotsResult.NextCursor, slotsResult.HasMore, slotsResult.Count);
    }
}
