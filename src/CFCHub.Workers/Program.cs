using CFCHub.Workers.Logging;
using OpenTelemetry;
using OpenTelemetry.Extensions.AWS.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using CFCHub.Application.Common.Telemetry;

var builder = Host.CreateApplicationBuilder(args);
LoggingConfiguration.ConfigureSerilog(builder);

Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("CFCHub.Workers"))
    .WithTracing(tracing =>
    {
        tracing.AddSource(AppActivitySource.Name)
               .SetSampler(new AlwaysOnSampler())
               .AddXRayTraceId()
               .AddEntityFrameworkCoreInstrumentation()
               .AddRedisInstrumentation()
               .AddAWSInstrumentation();

        if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
        else
        {
            tracing.AddOtlpExporter();
        }
    });

var host = builder.Build();
host.Run();
