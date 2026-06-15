using CFCHub.Api.Logging;

var builder = WebApplication.CreateBuilder(args);
LoggingConfiguration.ConfigureSerilog(builder);
var app = builder.Build();
app.Run();
