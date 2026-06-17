using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Compliance.Commands.RegisterDocument;
using CFCHub.Application.Compliance.Queries.GetExpiringDocuments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Compliance;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/compliance/documents");

        // POST /api/v1/compliance/documents
        group.MapPost("/", async (
            [FromBody] RegisterDocumentCommand command,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var result = await sender.Send(command);
            var uri = $"/api/v1/compliance/documents/{result}";
            return CFCHub.Domain.Shared.Result<string>.Success(result!).ToCreatedApiResponse(context, uri);
        });

        // GET /api/v1/compliance/documents/expiring
        group.MapGet("/expiring", async (
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to,
            [FromQuery] string? cursor,
            [FromQuery] int? limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var queryLimit = limit ?? 20;
            var query = new GetExpiringDocumentsQuery(from, to, queryLimit, cursor);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });
    }
}
