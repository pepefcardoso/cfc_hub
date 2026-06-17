using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Contracts.Commands.SignContract;
using CFCHub.Application.Contracts.Queries.GetContract;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Contracts;

public static class ContractEndpoints
{
    public static void MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts");

        // GET /api/v1/contracts/{contractId}
        group.MapGet("/{contractId:guid}", async (
            Guid contractId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var query = new GetContractQuery(contractId);
            var result = await sender.Send(query);
            return CFCHub.Domain.Shared.Result<ContractResult>.Success(result).ToApiResponse(context);
        });

        // PATCH /api/v1/contracts/{contractId}/sign
        group.MapPatch("/{contractId:guid}/sign", async (
            Guid contractId,
            [FromBody] SignContractRequest request,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var command = new SignContractCommand(contractId, request.SignatureHash);
            await sender.Send(command);
            return Results.Ok();
        });
    }
}

public record SignContractRequest(string SignatureHash);
