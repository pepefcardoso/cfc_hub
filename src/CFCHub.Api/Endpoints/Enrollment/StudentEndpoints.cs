using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Enrollment.Commands.CreateStudent;
using CFCHub.Application.Enrollment.Commands.RequestDataErasure;
using CFCHub.Application.Enrollment.Queries.GetStudent;
using CFCHub.Application.Enrollment.Queries.GetStudents;
using CFCHub.Application.Compliance.Queries.GetCnhStatus;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Enrollment;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/students");

        // POST /api/v1/students
        group.MapPost("/", async (
            [FromBody] CreateStudentCommand command,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var result = await sender.Send(command);
            var uri = $"/api/v1/students/{result.StudentId}";
            return CFCHub.Domain.Shared.Result<CreateStudentResult>.Success(result).ToCreatedApiResponse(context, uri);
        });

        // GET /api/v1/students
        group.MapGet("/", async (
            [FromQuery] string? cursor,
            [FromQuery] int? limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var queryLimit = limit ?? 20;
            var query = new GetStudentsQuery(queryLimit, cursor);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });

        // GET /api/v1/students/{studentId}
        group.MapGet("/{studentId:guid}", async (
            Guid studentId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var query = new GetStudentQuery(studentId);
            var result = await sender.Send(query);
            return CFCHub.Domain.Shared.Result<StudentResult>.Success(result).ToApiResponse(context);
        });

        // DELETE /api/v1/students/{studentId}
        group.MapDelete("/{studentId:guid}", async (
            Guid studentId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var command = new RequestDataErasureCommand(studentId);
            var result = await sender.Send(command);
            // Result is RequestDataErasureResult
            return Results.Accepted();
        });

    }
}
