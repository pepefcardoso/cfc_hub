using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
using System.Text.Json;

namespace CFCHub.Api.Endpoints.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Liveness
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false // Executes no checks, just returns 200 OK
        }).AllowAnonymous();

        // Readiness
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                var statusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    dependencies = report.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value.Status.ToString()
                    )
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }).AllowAnonymous();
    }
}
