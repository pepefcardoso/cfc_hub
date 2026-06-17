using CFCHub.Workers.Logging;
using OpenTelemetry;
using OpenTelemetry.Extensions.AWS.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using CFCHub.Application.Common.Telemetry;

using CFCHub.Workers.Common;
using CFCHub.Workers.Outbox;
using CFCHub.Workers.Compliance;
using CFCHub.Workers.Scheduling;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
LoggingConfiguration.ConfigureSerilog(builder);

Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

builder.Services.AddScoped<IOutboxMessageDispatcher, OutboxMessageDispatcher>();
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddHostedService<DocumentExpiryWorker>();
builder.Services.AddHostedService<SlotReminderWorker>();

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
namespace CFCHub.Workers { public partial class Program { } }
