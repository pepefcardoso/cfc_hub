using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Identity.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CFCHub.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (
            [FromBody] LoginCommand command,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var result = await sender.Send(command);
            return result.ToApiResponse(context);
        }).AllowAnonymous();
    }
}
