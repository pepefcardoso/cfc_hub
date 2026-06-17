using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Enrollment.Commands.EnrollStudent;
using CFCHub.Application.Enrollment.Queries.GetEnrollments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Enrollment;

public static class EnrollmentEndpoints
{
    public static void MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/students/{studentId:guid}/enrollments");

        // POST /api/v1/students/{studentId}/enrollments
        group.MapPost("/", async (
            Guid studentId,
            [FromBody] EnrollStudentCommand command,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            if (command.StudentId != studentId)
            {
                return Results.BadRequest("StudentId in route must match StudentId in body.");
            }

            var result = await sender.Send(command);
            var uri = $"/api/v1/students/{studentId}/enrollments/{result}";
            return CFCHub.Domain.Shared.Result<Guid>.Success(result).ToCreatedApiResponse(context, uri);
        });

        // GET /api/v1/students/{studentId}/enrollments
        group.MapGet("/", async (
            Guid studentId,
            [FromQuery] string? cursor,
            [FromQuery] int? limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var queryLimit = limit ?? 20;
            var query = new GetEnrollmentsQuery(studentId, queryLimit, cursor);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });
    }
}
