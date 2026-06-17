using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Scheduling.Commands.BookSlot;
using CFCHub.Application.Scheduling.Commands.CancelSlot;
using CFCHub.Application.Scheduling.Commands.CompleteSlot;
using CFCHub.Application.Scheduling.Commands.MarkNoShow;
using CFCHub.Application.Scheduling.Queries.GetAvailableSlots;
using CFCHub.Application.Scheduling.Queries.GetSlotById;
using CFCHub.Application.Scheduling.Queries.GetSlotsByStudent;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Scheduling;

public static class SchedulingEndpoints
{
    public static void MapSchedulingEndpoints(this IEndpointRouteBuilder app)
    {
        var schedulingGroup = app.MapGroup("/api/v1/scheduling");

        // GET /api/v1/scheduling/slots/available
        schedulingGroup.MapGet("/slots/available", async (
            [FromQuery] DateOnly date,
            [FromQuery] CnhCategory? category,
            [FromQuery] Guid? instructorId,
            [FromQuery] string? cursor,
            [FromQuery] int? limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var instId = instructorId.HasValue ? new InstructorId(instructorId.Value) : null;
            var queryLimit = limit ?? 20;
            var query = new GetAvailableSlotsQuery(date, category, instId, cursor, queryLimit);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });

        // POST /api/v1/scheduling/slots
        schedulingGroup.MapPost("/slots", async (
            [FromBody] BookSlotCommand command,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var result = await sender.Send(command);
            var uri = result.IsSuccess ? $"/api/v1/scheduling/slots/{result.Value.SchedulingSlotId}" : "";
            return result.ToCreatedApiResponse(context, uri);
        });

        // GET /api/v1/scheduling/slots/{slotId}
        schedulingGroup.MapGet("/slots/{slotId:guid}", async (
            Guid slotId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var query = new GetSlotByIdQuery(slotId);
            var result = await sender.Send(query);
            return result.ToApiResponse(context);
        });

        // PATCH /api/v1/scheduling/slots/{slotId}/cancel
        schedulingGroup.MapPatch("/slots/{slotId:guid}/cancel", async (
            Guid slotId,
            [FromBody] CancelSlotRequest request,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var command = new CancelSlotCommand(slotId, request.Reason);
            await sender.Send(command);
            return Results.Ok();
        });

        // PATCH /api/v1/scheduling/slots/{slotId}/complete
        schedulingGroup.MapPatch("/slots/{slotId:guid}/complete", async (
            Guid slotId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var command = new CompleteSlotCommand(slotId);
            await sender.Send(command);
            return Results.Ok();
        });

        // PATCH /api/v1/scheduling/slots/{slotId}/no-show
        schedulingGroup.MapPatch("/slots/{slotId:guid}/no-show", async (
            Guid slotId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var command = new MarkNoShowCommand(slotId);
            await sender.Send(command);
            return Results.Ok();
        });

        var studentGroup = app.MapGroup("/api/v1/students");

        // GET /api/v1/students/{studentId}/slots
        studentGroup.MapGet("/{studentId:guid}/slots", async (
            Guid studentId,
            [FromQuery] string? cursor,
            [FromQuery] int? limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var queryLimit = limit ?? 20;
            var query = new GetSlotsByStudentQuery(studentId, cursor, queryLimit);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });
    }
}

public record CancelSlotRequest(string Reason);
