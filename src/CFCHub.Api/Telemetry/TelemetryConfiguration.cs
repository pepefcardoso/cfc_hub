using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Extensions.AWS.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using CFCHub.Application.Common.Telemetry;

namespace CFCHub.Api.Telemetry;

public static class TelemetryConfiguration
{
    public static IServiceCollection AddCfcHubTelemetry(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        // Set X-Ray trace ID format
        Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("CFCHub"))
            .WithTracing(tracing =>
            {
                tracing.AddSource(AppActivitySource.Name)
                       .SetSampler(new AlwaysOnSampler())
                       .AddXRayTraceId()
                       .AddAspNetCoreInstrumentation(opt =>
                       {
                           opt.RecordException = true;
                       })
                       .AddEntityFrameworkCoreInstrumentation()
                       .AddRedisInstrumentation()
                       .AddAWSInstrumentation();

                if (env.IsDevelopment())
                {
                    tracing.AddConsoleExporter();
                }
                else
                {
                    tracing.AddOtlpExporter();
                }
            });

        return services;
    }
}
