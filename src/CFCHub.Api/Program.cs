using CFCHub.Api.Logging;
using CFCHub.Api.Telemetry;
using CFCHub.Application;
using CFCHub.Application.Common.Telemetry;
using CFCHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
LoggingConfiguration.ConfigureSerilog(builder);

builder.Services.AddCfcHubTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var orchestrator = scope.ServiceProvider.GetRequiredService<CFCHub.Infrastructure.Persistence.TenantMigrationOrchestrator>();
    await orchestrator.InitializeAsync();
}

app.UseMiddleware<CFCHub.Api.Middleware.SecurityHeadersMiddleware>();
app.UseMiddleware<CFCHub.Api.Middleware.GlobalExceptionMiddleware>();
app.UseMiddleware<CFCHub.Api.Middleware.TenantResolutionMiddleware>();
app.UseMiddleware<CFCHub.Api.Middleware.RateLimitMiddleware>();

app.Run();
