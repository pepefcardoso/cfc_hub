using System.Threading;
using CFCHub.Api.Logging;
using CFCHub.Api.Telemetry;
using CFCHub.Api.DependencyInjection;
using CFCHub.Application.Common.Telemetry;
using CFCHub.Api.Endpoints.Webhooks;
using CFCHub.Api.Endpoints.Auth;
using CFCHub.Api.Endpoints.Identity;
using CFCHub.Api.Endpoints.Scheduling;
using CFCHub.Api.Endpoints.Enrollment;
using CFCHub.Api.Endpoints.Contracts;
using CFCHub.Api.Endpoints.Finance;
using CFCHub.Api.Endpoints.Compliance;
using CFCHub.Api.Endpoints.Public;
using CFCHub.Api.Endpoints.Health;
using CFCHub.Api.Endpoints.Development;
using CFCHub.Infrastructure.Persistence;
using CFCHub.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);
LoggingConfiguration.ConfigureSerilog(builder);

builder.Services.AddCfcHubTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var orchestrator = scope.ServiceProvider.GetRequiredService<TenantMigrationOrchestrator>();
    await orchestrator.InitializeAsync(CancellationToken.None);
}

app.UseMiddleware<CFCHub.Api.Middleware.SecurityHeadersMiddleware>();
app.UseMiddleware<CFCHub.Api.Middleware.GlobalExceptionMiddleware>();
app.UseMiddleware<CFCHub.Api.Middleware.TenantResolutionMiddleware>();
app.UseMiddleware<CFCHub.Api.Middleware.RateLimitMiddleware>();

app.UseSwaggerConfiguration();

app.UseAuthentication();
app.UseAuthorization();

app.MapSesWebhooks();
app.MapAuthEndpoints();
app.MapStaffUserEndpoints();
app.MapSchedulingEndpoints();
app.MapInstructorEndpoints();
app.MapStudentEndpoints();
app.MapEnrollmentEndpoints();
app.MapContractEndpoints();
app.MapPaymentEndpoints();
app.MapDocumentEndpoints();
app.MapDetranEndpoints();
app.MapPublicEndpoints();
app.MapHealthEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapDevelopmentEndpoints();
}

app.Run();

namespace CFCHub.Api { public partial class Program { } }
