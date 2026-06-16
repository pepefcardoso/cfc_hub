using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Scheduling.Queries.GetAvailableSlots;
using CFCHub.Application.Scheduling.Queries.GetSlotsByInstructor;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Scheduling;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Scheduling;

public static class InstructorEndpoints
{
    public static void MapInstructorEndpoints(this IEndpointRouteBuilder app)
    {
        var instructorGroup = app.MapGroup("/api/v1/instructors");

        // GET /api/v1/instructors/{instructorId}/slots
        instructorGroup.MapGet("/{instructorId:guid}/slots", async (
            Guid instructorId,
            [FromQuery] string? cursor,
            [FromQuery] int? limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var queryLimit = limit ?? 20;
            var query = new GetSlotsByInstructorQuery(instructorId, cursor, queryLimit);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });

        // GET /api/v1/instructors/{instructorId}/availability
        instructorGroup.MapGet("/{instructorId:guid}/availability", async (
            Guid instructorId,
            [FromQuery] DateOnly date,
            [FromQuery] CnhCategory? category,
            [FromQuery] string? cursor,
            [FromQuery] int? limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var instId = new InstructorId(instructorId);
            var queryLimit = limit ?? 20;
            var query = new GetAvailableSlotsQuery(date, category, instId, cursor, queryLimit);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });
    }
}
