using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Compliance.Queries.GetCnhStatus;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Compliance;

public static class DetranEndpoints
{
    public static void MapDetranEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/students/{studentId:guid}/cnh-status");

        // GET /api/v1/students/{studentId}/cnh-status
        group.MapGet("/", async (
            Guid studentId,
            [FromQuery] string cpf,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var query = new GetCnhStatusQuery(cpf);
            var result = await sender.Send(query);
            return CFCHub.Domain.Shared.Result<CnhStatusResult>.Success(result).ToApiResponse(context);
        }).RequireRateLimiting("CnhStatus");
    }
}
