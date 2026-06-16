using System;
using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Identity.Commands.ChangeStaffUserRole;
using CFCHub.Application.Identity.Commands.CreateStaffUser;
using CFCHub.Application.Identity.Commands.DeactivateStaffUser;
using CFCHub.Application.Identity.Queries.GetStaffUsers;
using CFCHub.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CFCHub.Api.Endpoints.Identity;

public static class StaffUserEndpoints
{
    public static void MapStaffUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/staff-users");

        group.MapPost("", async (
            [FromBody] CreateStaffUserCommand command,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var result = await sender.Send(command);
            return result.ToCreatedApiResponse(context, $"/api/v1/staff-users/{result.Value?.Value}");
        });

        group.MapGet("", async (
            [FromQuery] string? cursor,
            [FromQuery] int limit,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var query = new GetStaffUsersQuery(cursor, limit == 0 ? 20 : limit);
            var result = await sender.Send(query);
            return result.ToApiPagedResponse(context);
        });

        group.MapPatch("/{userId:guid}/role", async (
            [FromRoute] Guid userId,
            [FromBody] ChangeStaffUserRoleRequest request,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var command = new ChangeStaffUserRoleCommand(userId, request.NewRole);
            var result = await sender.Send(command);
            return result.ToApiResponse(context);
        });

        group.MapPatch("/{userId:guid}/deactivate", async (
            [FromRoute] Guid userId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var command = new DeactivateStaffUserCommand(userId);
            var result = await sender.Send(command);
            return result.ToApiResponse(context);
        });
    }
}

public record ChangeStaffUserRoleRequest(RoleType NewRole);
