using CFCHub.Workers.Logging;

var builder = Host.CreateApplicationBuilder(args);
LoggingConfiguration.ConfigureSerilog(builder);
var host = builder.Build();
host.Run();
