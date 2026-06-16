using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Infrastructure.Caching;
using CFCHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CFCHub.Infrastructure.Scheduling;

public class AvailabilityCalculatorService : IAvailabilityCalculatorService
{
    private readonly AppDbContext _dbContext;
    private readonly IAvailabilityCacheService _cacheService;
    private readonly ISystemClock _clock;

    public AvailabilityCalculatorService(AppDbContext dbContext, IAvailabilityCacheService cacheService, ISystemClock clock)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _clock = clock;
    }

    public async Task<PagedResult<AvailableSlot>> GetAvailableSlotsAsync(
        DateOnly date,
        CnhCategory category,
        InstructorId? instructorId,
        string? cursor,
        int limit,
        CancellationToken ct)
    {
        var allSlots = new List<AvailableSlot>();

        if (instructorId != null)
        {
            var cached = await _cacheService.GetAsync(instructorId.Value.Value, date, ct);
            if (cached != null)
            {
                // We got it from cache, we can skip ALL db queries
                return ApplyPagination(cached.ToList(), cursor, limit);
            }
        }

        var instructorsQuery = _dbContext.Instructors.AsNoTracking();
        if (instructorId != null)
        {
            instructorsQuery = instructorsQuery.Where(i => i.Id == instructorId);
        }

        var instructors = await instructorsQuery.ToListAsync(ct);
        instructors = instructors.Where(i => i.TeachableCategories.Contains(category)).ToList();

        var uncachedInstructors = new List<Instructor>();

        foreach (var instructor in instructors)
        {
            var cached = await _cacheService.GetAsync(instructor.Id.Value, date, ct);
            if (cached != null)
            {
                allSlots.AddRange(cached);
            }
            else
            {
                uncachedInstructors.Add(instructor);
            }
        }

        if (uncachedInstructors.Any())
        {
            var vehicles = await _dbContext.Vehicles.AsNoTracking()
                .Where(v => v.Category == category && v.Status != VehicleStatus.Retired)
                .ToListAsync(ct);

            var tracks = await _dbContext.Tracks.AsNoTracking().ToListAsync(ct);

            var targetDateStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
            var targetDateEnd = targetDateStart.AddDays(1);

            var bookedSlots = await _dbContext.SchedulingSlots.AsNoTracking()
                .Where(s => s.StartedAt >= targetDateStart && s.StartedAt < targetDateEnd && s.Status != SlotStatus.Cancelled)
                .ToListAsync(ct);

            foreach (var instructor in uncachedInstructors)
            {
                var instructorSlots = new List<AvailableSlot>();

                for (int h = 0; h < 24; h++)
                {
                    var time = targetDateStart.AddHours(h);
                    if (!instructor.IsAvailableAt(time, _clock))
                        continue;

                    if (bookedSlots.Any(s => s.InstructorId == instructor.Id && s.StartedAt == time))
                        continue;

                    var vehicle = vehicles.FirstOrDefault(v => 
                        v.IsAvailableAt(time) && 
                        !bookedSlots.Any(s => s.VehicleId == v.Id && s.StartedAt == time));

                    if (vehicle == null) continue;

                    var requiredTrackType = category == CnhCategory.A ? TrackType.Maneuver : TrackType.Road;
                    var track = tracks.FirstOrDefault(t => 
                        t.Type == requiredTrackType && 
                        !bookedSlots.Any(s => s.TrackId == t.Id && s.StartedAt == time));

                    if (track == null) continue;

                    instructorSlots.Add(new AvailableSlot(
                        time,
                        instructor.Id,
                        instructor.Name,
                        vehicle.Id,
                        track.Id,
                        track.Type
                    ));
                }

                await _cacheService.SetAsync(instructor.Id.Value, date, instructorSlots, ct);
                allSlots.AddRange(instructorSlots);
            }
        }

        return ApplyPagination(allSlots, cursor, limit);
    }

    private PagedResult<AvailableSlot> ApplyPagination(List<AvailableSlot> allSlots, string? cursor, int limit)
    {
        var sortedSlots = allSlots.OrderBy(s => s.StartedAt).ThenBy(s => s.InstructorId.Value).ToList();

        if (!string.IsNullOrEmpty(cursor))
        {
            try 
            {
                var cursorParts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split('|');
                if (cursorParts.Length == 2 && long.TryParse(cursorParts[0], out var ticks) && Guid.TryParse(cursorParts[1], out var lastInstructorId))
                {
                    sortedSlots = sortedSlots.Where(s => 
                        s.StartedAt.Ticks > ticks || 
                        (s.StartedAt.Ticks == ticks && s.InstructorId.Value.CompareTo(lastInstructorId) > 0)).ToList();
                }
            }
            catch
            {
                // Ignore invalid cursor
            }
        }

        var pagedItems = sortedSlots.Take(limit).ToList();
        var hasMore = sortedSlots.Count > limit;
        string? nextCursor = null;

        if (pagedItems.Any())
        {
            var last = pagedItems.Last();
            nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{last.StartedAt.Ticks}|{last.InstructorId.Value}"));
        }

        return new PagedResult<AvailableSlot>(pagedItems, nextCursor, hasMore, pagedItems.Count);
    }
}
