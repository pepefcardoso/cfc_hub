using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Admin.Queries.GetPublicCfcInfo;
using CFCHub.Application.Admin.Queries.GetQrCodeInfo;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CFCHub.Api.Endpoints.Public;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/public");

        // GET /api/v1/public/cfc/{slug}
        group.MapGet("/cfc/{slug}", async (
            string slug,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var query = new GetPublicCfcInfoQuery(slug);
            var result = await sender.Send(query);
            return result.ToApiResponse(context);
        }).AllowAnonymous().RequireRateLimiting("Public60");

        // GET /api/v1/public/qr/{code}
        group.MapGet("/qr/{code}", async (
            string code,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var query = new GetQrCodeInfoQuery(code);
            var result = await sender.Send(query);
            return result.ToApiResponse(context);
        }).AllowAnonymous().RequireRateLimiting("Public60");
    }
}
