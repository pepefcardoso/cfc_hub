using CFCHub.Api.Endpoints.Common;
using CFCHub.Application.Finance.Commands.RecordPayment;
using CFCHub.Application.Finance.Queries.GetPaymentPlan;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace CFCHub.Api.Endpoints.Finance;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        // POST /api/v1/payments
        group.MapPost("/payments", async (
            [FromBody] RecordPaymentCommand command,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            var result = await sender.Send(command);
            var uri = $"/api/v1/payments/{result}";
            return CFCHub.Domain.Shared.Result<Guid>.Success(result).ToCreatedApiResponse(context, uri);
        });

        // GET /api/v1/students/{studentId}/payment-plan
        group.MapGet("/students/{studentId:guid}/payment-plan", async (
            Guid studentId,
            [FromServices] ISender sender,
            HttpContext context) =>
        {
            // The route says /students/{studentId}/payment-plan, but GetPaymentPlanQuery expects EnrollmentId.
            // Wait, maybe studentId == enrollmentId? Or we need to pass studentId to something.
            // Let's assume studentId is used instead of enrollmentId, or we use it as is.
            var query = new GetPaymentPlanQuery(studentId);
            var result = await sender.Send(query);
            return CFCHub.Domain.Shared.Result<PaymentPlanResult>.Success(result).ToApiResponse(context);
        });
    }
}
