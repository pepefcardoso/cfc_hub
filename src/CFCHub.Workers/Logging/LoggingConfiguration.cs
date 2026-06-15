using System;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.AwsCloudWatch;
using Amazon.CloudWatchLogs;

namespace CFCHub.Workers.Logging;

public static class LoggingConfiguration
{
    public static void ConfigureSerilog(HostApplicationBuilder builder)
    {
        var env = Environment.GetEnvironmentVariable("CFCHUB_ENVIRONMENT") ?? "dev";
        var isDev = env.Equals("dev", StringComparison.OrdinalIgnoreCase);

        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("environment", env)
            .Destructure.With<Api.Logging.SensitiveDataDestructuringPolicy>();

        if (isDev)
        {
            loggerConfig
                .MinimumLevel.Debug()
                .WriteTo.Seq("http://seq:5341");
        }
        else
        {
            loggerConfig
                .MinimumLevel.Information()
                .Filter.ByExcluding(e => e.Level == LogEventLevel.Debug)
                .WriteTo.AmazonCloudWatch(new CloudWatchSinkOptions
                {
                    LogGroupName = $"/cfchub/{env}/workers",
                    MinimumLogEventLevel = LogEventLevel.Information
                }, new AmazonCloudWatchLogsClient());
        }

        builder.Services.AddSerilog(loggerConfig.CreateLogger());
    }
}
