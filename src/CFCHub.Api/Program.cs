using CFCHub.Api.Logging;
using CFCHub.Api.Telemetry;
using CFCHub.Application.Common.Telemetry;

var builder = WebApplication.CreateBuilder(args);
LoggingConfiguration.ConfigureSerilog(builder);

builder.Services.AddCfcHubTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.Run();
